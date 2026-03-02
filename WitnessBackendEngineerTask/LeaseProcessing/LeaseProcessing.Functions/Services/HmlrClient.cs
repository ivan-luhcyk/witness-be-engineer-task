using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using LeaseProcessing.Functions.Models;
using LeaseProcessing.Functions.Options;
using Microsoft.Extensions.Options;
using WitnessBackendEngineerTask.Common.Resilience;

namespace LeaseProcessing.Functions.Services;

/// <summary>
/// Pulls raw schedule data from HMLR and applies transient-failure retries.
/// </summary>
public sealed class HmlrClient : IHmlrClient
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly HmlrOptions _options;
    private readonly IRetryRunner _retryRunner;
    private readonly RetrySettings _retrySettings;

    public HmlrClient(
        HttpClient httpClient,
        IOptions<HmlrOptions> options,
        IOptions<HmlrRetryOptions> retryOptions,
        IRetryRunner retryRunner)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _retryRunner = retryRunner;
        _retrySettings = new RetrySettings
        {
            MaxAttempts = retryOptions.Value.MaxAttempts,
            InitialDelayMs = retryOptions.Value.InitialDelayMs,
            MaxDelayMs = retryOptions.Value.MaxDelayMs
        };
    }

    public async Task<IReadOnlyList<RawScheduleNoticeOfLease>> GetRawSchedulesAsync(CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);

        var uri = new Uri(new Uri(_options.BaseUrl), "/schedules");
        var content = await _retryRunner.ExecuteAsync(
            async () =>
            {
                using var response = await _httpClient.GetAsync(uri, cancellationToken);

                // Promote transient HTTP status codes to exceptions so RetryRunner can back off/retry.
                if (IsTransientStatusCode(response.StatusCode))
                {
                    throw new HttpRequestException(
                        $"Transient HMLR response {(int)response.StatusCode}.",
                        null,
                        response.StatusCode);
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken);
            },
            _retrySettings,
            IsTransient,
            "HMLR.GetSchedules",
            cancellationToken);

        var parsed = JsonSerializer.Deserialize<List<RawScheduleNoticeOfLease>>(content, _jsonOptions);
        return parsed ?? [];
    }

    private bool IsTransient(Exception ex) =>
        ex is HttpRequestException httpEx && IsTransientStatusCode(httpEx.StatusCode)
        || ex is TaskCanceledException
        || ex is TimeoutException;

    private bool IsTransientStatusCode(HttpStatusCode? statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
