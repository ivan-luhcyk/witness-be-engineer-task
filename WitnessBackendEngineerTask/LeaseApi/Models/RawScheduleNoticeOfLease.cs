namespace LeaseApi.Models;

public record RawScheduleNoticeOfLease(
    string EntryNumber,
    string EntryDate,
    string EntryType,
    List<string> EntryText
);
