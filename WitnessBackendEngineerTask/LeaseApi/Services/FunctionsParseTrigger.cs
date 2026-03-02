using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Security.Cryptography;
using LeaseApi.Options;
using Microsoft.Extensions.Options;
using WitnessBackendEngineerTask.Common.Models;

namespace LeaseApi.Services;

/// <summary>
/// Sends a signed parse trigger request from LeaseApi to the Function endpoint.
/// </summary>
public sealed class FunctionsParseTrigger : IParseTrigger
{
    private readonly HttpClient _httpClient;
    private readonly ParserOptions _options;

    public FunctionsParseTrigger(HttpClient httpClient, IOptions<ParserOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task TriggerAsync(string requestedTitleNumber, CancellationToken cancellationToken)
    {
        var uri = new Uri(new Uri(_options.BaseUrl), _options.TriggerPath);
        var payload = JsonSerializer.Serialize(new ParseRequest(requestedTitleNumber));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var nonce = Guid.NewGuid().ToString("N");
        // Signature is computed over timestamp + nonce + payload to prevent tampering and replay.
        var signature = BuildSignature(timestamp, nonce, payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation(_options.TimestampHeaderName, timestamp);
        request.Headers.TryAddWithoutValidation(_options.NonceHeaderName, nonce);
        request.Headers.TryAddWithoutValidation(_options.SignatureHeaderName, signature);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private string BuildSignature(string timestamp, string nonce, string payload)
    {
        var material = $"{timestamp}.{nonce}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.ServiceToken));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
