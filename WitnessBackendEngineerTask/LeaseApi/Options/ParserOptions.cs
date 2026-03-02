namespace LeaseApi.Options;

public sealed class ParserOptions
{
    public const string SectionName = "Parser";

    public string BaseUrl { get; init; } = "http://lease-processing";
    public string TriggerPath { get; init; } = "/api/parse";
    public string ServiceToken { get; init; } = "change-me-in-config";
    public string TimestampHeaderName { get; init; } = "X-Request-Timestamp";
    public string NonceHeaderName { get; init; } = "X-Request-Nonce";
    public string SignatureHeaderName { get; init; } = "X-Request-Signature";
}
