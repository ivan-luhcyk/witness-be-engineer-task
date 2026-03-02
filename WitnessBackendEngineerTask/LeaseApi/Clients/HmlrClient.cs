using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeaseApi.Models;
using LeaseApi.Options;
using Microsoft.Extensions.Options;

namespace LeaseApi.Clients;

public sealed class HmlrClient : IHmlrClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly HmlrOptions _options;
    private readonly ILogger<HmlrClient> _logger;

    public HmlrClient(HttpClient httpClient, IOptions<HmlrOptions> options, ILogger<HmlrClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RawScheduleNoticeOfLease>> GetSchedulesAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("Hmlr:BaseUrl is not configured.");
        }

        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        var requestUri = new Uri(new Uri(_options.BaseUrl, UriKind.Absolute), "/schedules");
        _logger.LogInformation("Requesting HMLR schedules from {Url}", requestUri);
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("HMLR request failed with status {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException(
                $"HMLR request failed with status {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        var scheduleItems = JsonSerializer.Deserialize<List<RawScheduleNoticeOfLease>>(content, JsonOptions);
        if (scheduleItems is null)
        {
            _logger.LogWarning("HMLR response could not be deserialized.");
            throw new InvalidOperationException("Failed to deserialize HMLR schedule data.");
        }

        _logger.LogInformation("Received {Count} schedule items from HMLR.", scheduleItems.Count);
        return scheduleItems;
    }
}
