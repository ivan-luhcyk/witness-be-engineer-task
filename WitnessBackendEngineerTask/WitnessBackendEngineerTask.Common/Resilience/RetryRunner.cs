using Microsoft.Extensions.Logging;

namespace WitnessBackendEngineerTask.Common.Resilience;

public sealed class RetryRunner : IRetryRunner
{
    private readonly ILogger<RetryRunner> _logger;

    public RetryRunner(ILogger<RetryRunner> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Func<Task> operation,
        RetrySettings settings,
        Func<Exception, bool> isTransient,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            async () =>
            {
                await operation();
                return true;
            },
            settings,
            isTransient,
            operationName,
            cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        RetrySettings settings,
        Func<Exception, bool> isTransient,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastError = null;

        while (attempt < settings.MaxAttempts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            try
            {
                return await operation();
            }
            catch (Exception ex) when (isTransient(ex))
            {
                lastError = ex;
                if (attempt >= settings.MaxAttempts)
                {
                    break;
                }

                var delay = CalculateDelay(settings, attempt);
                _logger.LogWarning(ex, "Transient error in {OperationName} on attempt {Attempt}. Retrying in {DelayMs} ms.", operationName, attempt, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastError ?? new InvalidOperationException($"Retry execution failed for operation {operationName} without exception.");
    }

    private TimeSpan CalculateDelay(RetrySettings settings, int attempt)
    {
        var delayMs = settings.InitialDelayMs * Math.Pow(2, Math.Max(0, attempt - 1));
        return TimeSpan.FromMilliseconds(Math.Min(delayMs, settings.MaxDelayMs));
    }
}
