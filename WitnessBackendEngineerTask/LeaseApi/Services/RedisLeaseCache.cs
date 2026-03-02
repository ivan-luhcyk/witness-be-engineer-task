using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WitnessBackendEngineerTask.Common.Models;
using WitnessBackendEngineerTask.Common.Options;
using WitnessBackendEngineerTask.Common.Resilience;

namespace LeaseApi.Services;

/// <summary>
/// Redis-backed cache for parsed lease results and processing status.
/// </summary>
public sealed class RedisLeaseCache : ILeaseCache
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private const string ParseLockKey = "lease:parse:lock";

    private readonly RedisOptions _options;
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IRetryRunner _retryRunner;
    private readonly RetrySettings _retrySettings;

    public RedisLeaseCache(
        IOptions<RedisOptions> options,
        IOptions<RedisRetryOptions> retryOptions,
        IConnectionMultiplexer multiplexer,
        IRetryRunner retryRunner)
    {
        _options = options.Value;
        _multiplexer = multiplexer;
        _database = multiplexer.GetDatabase();
        _retryRunner = retryRunner;
        _retrySettings = new RetrySettings
        {
            MaxAttempts = retryOptions.Value.MaxAttempts,
            InitialDelayMs = retryOptions.Value.InitialDelayMs,
            MaxDelayMs = retryOptions.Value.MaxDelayMs
        };
    }

    public async Task<ParsedScheduleNoticeOfLease?> GetResultAsync(string titleNumber)
    {
        var value = await _retryRunner.ExecuteAsync(
            () => _database.StringGetAsync(ResultKey(titleNumber)),
            _retrySettings,
            IsRedisTransient,
            "Redis.StringGet(Result)");
        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<ParsedScheduleNoticeOfLease>(value!, _jsonOptions);
    }

    public async Task<IReadOnlyList<ParsedScheduleNoticeOfLease>> GetAllResultsAsync()
    {
        var uniqueKeys = new HashSet<string>(StringComparer.Ordinal);
        var keyPattern = $"{_options.ResultKeyPrefix}*";

        // Enumerate keys per endpoint because StackExchange.Redis requires server-level key scan.
        foreach (var endpoint in _multiplexer.GetEndPoints())
        {
            var server = _multiplexer.GetServer(endpoint);
            if (!server.IsConnected)
            {
                continue;
            }

            foreach (var key in server.Keys(pattern: keyPattern))
            {
                var keyString = key.ToString();
                if (!string.IsNullOrWhiteSpace(keyString))
                {
                    uniqueKeys.Add(keyString);
                }
            }
        }

        if (uniqueKeys.Count == 0)
        {
            return [];
        }

        var redisKeys = uniqueKeys.Select(k => (RedisKey)k).ToArray();
        var values = await _retryRunner.ExecuteAsync(
            () => _database.StringGetAsync(redisKeys),
            _retrySettings,
            IsRedisTransient,
            "Redis.StringGet(MultiResult)");

        var results = new List<ParsedScheduleNoticeOfLease>(values.Length);
        foreach (var value in values)
        {
            if (value.IsNullOrEmpty)
            {
                continue;
            }

            var item = JsonSerializer.Deserialize<ParsedScheduleNoticeOfLease>(value!, _jsonOptions);
            if (item is not null)
            {
                results.Add(item);
            }
        }

        return results;
    }

    public Task SetResultAsync(ParsedScheduleNoticeOfLease result) =>
        _retryRunner.ExecuteAsync(
            () => _database.StringSetAsync(ResultKey(result.LesseesTitle), JsonSerializer.Serialize(result, _jsonOptions)),
            _retrySettings,
            IsRedisTransient,
            "Redis.StringSet(Result)");

    public async Task<LeaseProcessingStatus?> GetStatusAsync(string titleNumber)
    {
        var value = await _retryRunner.ExecuteAsync(
            () => _database.StringGetAsync(StatusKey(titleNumber)),
            _retrySettings,
            IsRedisTransient,
            "Redis.StringGet(Status)");
        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<LeaseProcessingStatus>(value!, _jsonOptions);
    }

    public Task SetStatusAsync(LeaseProcessingStatus status) =>
        _retryRunner.ExecuteAsync(
            () => _database.StringSetAsync(StatusKey(status.TitleNumber), JsonSerializer.Serialize(status, _jsonOptions)),
            _retrySettings,
            IsRedisTransient,
            "Redis.StringSet(Status)");

    public Task<bool> TryAcquireParseTriggerLockAsync(TimeSpan ttl) =>
        _retryRunner.ExecuteAsync(
            // SET NX ensures only one trigger call is issued for a short window.
            () => _database.StringSetAsync(ParseLockKey, DateTimeOffset.UtcNow.ToString("O"), expiry: ttl, when: When.NotExists),
            _retrySettings,
            IsRedisTransient,
            "Redis.StringSet(ParseLock)");

    private string StatusKey(string titleNumber) => $"{_options.StatusKeyPrefix}{titleNumber.ToUpperInvariant()}";
    private string ResultKey(string titleNumber) => $"{_options.ResultKeyPrefix}{titleNumber.ToUpperInvariant()}";

    private bool IsRedisTransient(Exception ex) =>
        ex is RedisTimeoutException or RedisConnectionException or TimeoutException or RedisException;
}
