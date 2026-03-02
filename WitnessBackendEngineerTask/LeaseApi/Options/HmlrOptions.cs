namespace LeaseApi.Options;

public record HmlrOptions
{
    public const string SectionName = "Hmlr";
    public string BaseUrl { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
