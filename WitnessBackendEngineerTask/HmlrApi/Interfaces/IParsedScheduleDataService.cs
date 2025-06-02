using HmlrApi.Models.DTOs;

namespace HmlrApi.Interfaces;

public interface IParsedScheduleDataService
{
    public IEnumerable<ParsedScheduleNoticeOfLease> GetParsedScheduleNoticeOfLeases();
}