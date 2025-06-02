using HmlrApi.Models.DTOs;

namespace HmlrApi.Interfaces;

public interface IRawScheduleDataService
{
    IEnumerable<RawScheduleNoticeOfLease> GetRawScheduleNoticeOfLeases();
}