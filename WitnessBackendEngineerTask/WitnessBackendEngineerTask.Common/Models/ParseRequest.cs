namespace WitnessBackendEngineerTask.Common.Models;

/// <summary>
/// Contract used by LeaseApi to trigger parsing in the Function host.
/// </summary>
/// <param name="TitleNumber">
/// Optional caller context title; the function still parses the full HMLR list.
/// </param>
public sealed record ParseRequest(string? TitleNumber);
