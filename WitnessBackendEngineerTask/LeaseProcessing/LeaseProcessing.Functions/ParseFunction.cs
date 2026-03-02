using System.Net;
using System.IO;
using System.Text.Json;
using LeaseProcessing.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WitnessBackendEngineerTask.Common.Models;

namespace LeaseProcessing.Functions;

public sealed class ParseFunction
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILeaseStore _store;
    private readonly IHmlrClient _hmlrClient;
    private readonly IScheduleParser _parser;
    private readonly IParseRequestAuthenticator _authenticator;
    private readonly ILogger<ParseFunction> _logger;

    public ParseFunction(
        ILeaseStore store,
        IHmlrClient hmlrClient,
        IScheduleParser parser,
        IParseRequestAuthenticator authenticator,
        ILogger<ParseFunction> logger)
    {
        _store = store;
        _hmlrClient = hmlrClient;
        _parser = parser;
        _authenticator = authenticator;
        _logger = logger;
    }

    [Function("Parse")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "parse")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var payloadText = await ReadBodyAsync(request, cancellationToken);
        if (!await _authenticator.IsAuthorizedAsync(request, payloadText, cancellationToken))
        {
            var unauthorized = request.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorized.WriteStringAsync("Unauthorized.", cancellationToken);
            return unauthorized;
        }

        var payload = JsonSerializer.Deserialize<ParseRequest>(payloadText, _jsonOptions);
        var requestedTitle = payload?.TitleNumber?.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(requestedTitle))
        {
            await _store.SetStatusAsync(new LeaseProcessingStatus(requestedTitle, LeaseProcessingStatus.ProcessingState, null));
        }

        try
        {
            var rawSchedules = await _hmlrClient.GetRawSchedulesAsync(cancellationToken);
            var parsedSchedules = _parser.Parse(rawSchedules);

            foreach (var parsed in parsedSchedules)
            {
                if (string.IsNullOrWhiteSpace(parsed.LesseesTitle))
                {
                    continue;
                }

                parsed.LesseesTitle = parsed.LesseesTitle.ToUpperInvariant();
                await _store.SetResultAsync(parsed);
                await _store.SetStatusAsync(new LeaseProcessingStatus(parsed.LesseesTitle, LeaseProcessingStatus.CompletedState, null));
            }

            if (!string.IsNullOrWhiteSpace(requestedTitle) &&
                !parsedSchedules.Any(x => string.Equals(x.LesseesTitle, requestedTitle, StringComparison.OrdinalIgnoreCase)))
            {
                await _store.SetStatusAsync(new LeaseProcessingStatus(requestedTitle, LeaseProcessingStatus.FailedState, "Title number not found."));
            }

            var ok = request.CreateResponse(HttpStatusCode.OK);
            await ok.WriteStringAsync("Parse-all completed.", cancellationToken);
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parse-all failed");
            if (!string.IsNullOrWhiteSpace(requestedTitle))
            {
                await _store.SetStatusAsync(new LeaseProcessingStatus(requestedTitle, LeaseProcessingStatus.FailedState, ex.Message));
            }

            var failed = request.CreateResponse(HttpStatusCode.InternalServerError);
            await failed.WriteStringAsync("Parse-all failed.", cancellationToken);
            return failed;
        }
    }

    private async Task<string> ReadBodyAsync(HttpRequestData request, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.Body);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
