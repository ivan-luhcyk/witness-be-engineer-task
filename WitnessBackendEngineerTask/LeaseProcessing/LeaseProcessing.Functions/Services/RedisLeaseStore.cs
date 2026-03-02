using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WitnessBackendEngineerTask.Common.Models;
using WitnessBackendEngineerTask.Common.Options;
using WitnessBackendEngineerTask.Common.Resilience;

namespace LeaseProcessing.Functions.Services;

/// <summary>
/// Persists function processing outputs in Redis with transient retry handling.
/// </summary>
public sealed class RedisLeaseStore : ILeaseStore
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RedisOptions _options;
    private readonly IDatabase _database;
    private readonly IRetryRunner _retryRunner;
    private readonly RetrySettings _retrySettings;

    public RedisLeaseStore(
        IOptions<RedisOptions> options,
        IOptions<RedisRetryOptions> retryOptions,
        IConnectionMultiplexer multiplexer,
        IRetryRunner retryRunner)
    {
        _options = options.Value;
        _database = multiplexer.GetDatabase();
        _retryRunner = retryRunner;
        _retrySettings = new RetrySettings
        {
            MaxAttempts = retryOptions.Value.MaxAttempts,
            InitialDelayMs = retryOptions.Value.InitialDelayMs,
            MaxDelayMs = retryOptions.Value.MaxDelayMs
        };
    }

    public Task SetStatusAsync(LeaseProcessingStatus status) =>
        _retryRunner.ExecuteAsync(
            () => _database.StringSetAsync(StatusKey(status.TitleNumber), JsonSerializer.Serialize(status, _jsonOptions)),
            _retrySettings,
            IsRedisTransient,
            "Redis.StringSet(Status)");

    public Task SetResultAsync(ParsedScheduleNoticeOfLease result) =>
        _retryRunner.ExecuteAsync(
            () => _database.StringSetAsync(ResultKey(result.LesseesTitle), JsonSerializer.Serialize(result, _jsonOptions)),
            _retrySettings,
            IsRedisTransient,
            "Redis.StringSet(Result)");

    private string StatusKey(string titleNumber) => $"{_options.StatusKeyPrefix}{titleNumber.ToUpperInvariant()}";
    private string ResultKey(string titleNumber) => $"{_options.ResultKeyPrefix}{titleNumber.ToUpperInvariant()}";

    private bool IsRedisTransient(Exception ex) =>
        ex is RedisTimeoutException or RedisConnectionException or TimeoutException or RedisException;
}
