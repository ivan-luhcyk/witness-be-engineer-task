namespace WitnessBackendEngineerTask.Common.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = "localhost:6379";
    public string StatusKeyPrefix { get; init; } = "lease:status:";
    public string ResultKeyPrefix { get; init; } = "lease:result:";
}
