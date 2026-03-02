namespace LeaseProcessing.Functions.Models;

public sealed class RawScheduleNoticeOfLease
{
    public string EntryNumber { get; set; } = string.Empty;
    public string EntryDate { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public List<string> EntryText { get; set; } = [];
}
