namespace WitnessBackendEngineerTask.Common.Resilience;

public sealed class RetrySettings
{
    public int MaxAttempts { get; init; } = 3;
    public int InitialDelayMs { get; init; } = 100;
    public int MaxDelayMs { get; init; } = 1000;
}
