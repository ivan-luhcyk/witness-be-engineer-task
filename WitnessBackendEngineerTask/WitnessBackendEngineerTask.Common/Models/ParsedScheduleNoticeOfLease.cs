namespace WitnessBackendEngineerTask.Common.Models;

/// <summary>
/// Parsed lease record returned by LeaseApi and stored in Redis.
/// </summary>
public sealed class ParsedScheduleNoticeOfLease
{
    /// <summary>Original entry number in the HMLR schedule.</summary>
    public int EntryNumber { get; set; }
    /// <summary>Parsed entry date from the source data when valid.</summary>
    public DateOnly? EntryDate { get; set; }
    /// <summary>Registration date and plan reference section.</summary>
    public string RegistrationDateAndPlanRef { get; set; } = string.Empty;
    /// <summary>Property description section.</summary>
    public string PropertyDescription { get; set; } = string.Empty;
    /// <summary>Date of lease and term section.</summary>
    public string DateOfLeaseAndTerm { get; set; } = string.Empty;
    /// <summary>Extracted lessee title number used as cache key.</summary>
    public string LesseesTitle { get; set; } = string.Empty;
    /// <summary>Any NOTE lines collected from the raw entry.</summary>
    public List<string> Notes { get; set; } = [];
}
