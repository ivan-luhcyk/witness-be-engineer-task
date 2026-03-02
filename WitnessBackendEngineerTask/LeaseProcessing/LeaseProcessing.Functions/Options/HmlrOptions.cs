namespace LeaseProcessing.Functions.Options;

public sealed class HmlrOptions
{
    public const string SectionName = "Hmlr";

    public string BaseUrl { get; init; } = "http://hmlr-api:8080";
    public string Username { get; init; } = "username";
    public string Password { get; init; } = "password";
}
