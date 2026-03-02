using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LeaseProcessing.Functions.Options;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WitnessBackendEngineerTask.Common.Options;
using WitnessBackendEngineerTask.Common.Resilience;

namespace LeaseProcessing.Functions.Services;

/// <summary>
/// Validates signed parse requests and enforces nonce replay protection.
/// </summary>
public sealed class HmacParseRequestAuthenticator : IParseRequestAuthenticator
{
    private readonly ParserAuthOptions _options;
    private readonly IDatabase _database;
    private readonly IRetryRunner _retryRunner;
    private readonly RetrySettings _retrySettings;

    public HmacParseRequestAuthenticator(
        IOptions<ParserAuthOptions> options,
        IConnectionMultiplexer multiplexer,
        IOptions<RedisRetryOptions> retryOptions,
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

    public async Task<bool> IsAuthorizedAsync(HttpRequestData request, string payload, CancellationToken cancellationToken)
    {
        if (!TryGetHeaderValue(request, _options.TimestampHeaderName, out var timestampRaw))
        {
            return false;
        }

        if (!TryGetHeaderValue(request, _options.NonceHeaderName, out var nonce))
        {
            return false;
        }

        if (!TryGetHeaderValue(request, _options.SignatureHeaderName, out var incomingSignature))
        {
            return false;
        }

        if (!long.TryParse(timestampRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestampSeconds))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // Reject stale/future requests outside the allowed clock skew window.
        if (Math.Abs(now - timestampSeconds) > _options.AllowedClockSkewSeconds)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(nonce))
        {
            return false;
        }

        if (!IsSignatureValid(timestampRaw, nonce, payload, incomingSignature))
        {
            return false;
        }

        var nonceKey = $"{_options.NonceKeyPrefix}{nonce}";
        var ttl = TimeSpan.FromSeconds(_options.AllowedClockSkewSeconds);
        var nonceStored = await _retryRunner.ExecuteAsync(
            // SET NX makes each nonce one-time-use within the skew window.
            () => _database.StringSetAsync(nonceKey, "1", ttl, When.NotExists),
            _retrySettings,
            IsRedisTransient,
            "Redis.StringSet(Nonce)",
            cancellationToken);
        return nonceStored;
    }

    private bool TryGetHeaderValue(HttpRequestData request, string headerName, out string value)
    {
        value = string.Empty;
        if (!request.Headers.TryGetValues(headerName, out var values))
        {
            return false;
        }

        var first = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first))
        {
            return false;
        }

        value = first;
        return true;
    }

    private bool IsSignatureValid(string timestamp, string nonce, string payload, string incomingSignature)
    {
        if (string.IsNullOrWhiteSpace(incomingSignature))
        {
            return false;
        }

        var material = $"{timestamp}.{nonce}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.ServiceToken));
        var expectedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(material));

        byte[] incomingBytes;
        try
        {
            incomingBytes = Convert.FromHexString(incomingSignature.Trim());
        }
        catch
        {
            return false;
        }

        return incomingBytes.Length == expectedBytes.Length &&
               // Constant-time comparison avoids timing side-channel leaks.
               CryptographicOperations.FixedTimeEquals(incomingBytes, expectedBytes);
    }

    private bool IsRedisTransient(Exception ex) =>
        ex is RedisTimeoutException or RedisConnectionException or TimeoutException or RedisException;
}
