namespace WitnessBackendEngineerTask.Common.Resilience;

public interface IRetryRunner
{
    Task ExecuteAsync(
        Func<Task> operation,
        RetrySettings settings,
        Func<Exception, bool> isTransient,
        string operationName,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        RetrySettings settings,
        Func<Exception, bool> isTransient,
        string operationName,
        CancellationToken cancellationToken = default);
}
