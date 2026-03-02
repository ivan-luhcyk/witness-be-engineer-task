namespace LeaseProcessing.Functions.Options;

public sealed class HmlrRetryOptions
{
    public const string SectionName = "HmlrRetry";

    public int MaxAttempts { get; init; } = 3;
    public int InitialDelayMs { get; init; } = 200;
    public int MaxDelayMs { get; init; } = 2000;
}
