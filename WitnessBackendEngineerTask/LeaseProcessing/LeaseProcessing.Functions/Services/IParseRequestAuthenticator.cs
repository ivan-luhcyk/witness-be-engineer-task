using Microsoft.Azure.Functions.Worker.Http;

namespace LeaseProcessing.Functions.Services;

public interface IParseRequestAuthenticator
{
    Task<bool> IsAuthorizedAsync(HttpRequestData request, string payload, CancellationToken cancellationToken);
}
