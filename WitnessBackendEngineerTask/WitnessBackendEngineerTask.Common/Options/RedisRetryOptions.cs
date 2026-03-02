namespace WitnessBackendEngineerTask.Common.Options;

public sealed class RedisRetryOptions
{
    public const string SectionName = "RedisRetry";

    public int MaxAttempts { get; init; } = 3;
    public int InitialDelayMs { get; init; } = 100;
    public int MaxDelayMs { get; init; } = 1000;
}
