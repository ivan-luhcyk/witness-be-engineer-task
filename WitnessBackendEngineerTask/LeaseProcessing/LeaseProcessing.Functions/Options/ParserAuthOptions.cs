namespace LeaseProcessing.Functions.Options;

public sealed class ParserAuthOptions
{
    public const string SectionName = "ParserAuth";

    public string ServiceToken { get; init; } = "change-me-in-config";
    public string TimestampHeaderName { get; init; } = "X-Request-Timestamp";
    public string NonceHeaderName { get; init; } = "X-Request-Nonce";
    public string SignatureHeaderName { get; init; } = "X-Request-Signature";
    public string NonceKeyPrefix { get; init; } = "lease:nonce:";
    public int AllowedClockSkewSeconds { get; init; } = 300;
}
