namespace LeaseApi.Services;

public interface IParseTrigger
{
    Task TriggerAsync(string requestedTitleNumber, CancellationToken cancellationToken);
}
