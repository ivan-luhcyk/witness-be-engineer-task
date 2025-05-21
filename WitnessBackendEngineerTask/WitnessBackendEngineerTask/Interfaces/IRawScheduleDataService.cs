using System.Collections.Generic;
using WitnessBackendEngineerTask.Models.DTOs;

namespace WitnessBackendEngineerTask.Interfaces;

public interface IRawScheduleDataService
{
    IEnumerable<RawScheduleNoticeOfLease> GetRawScheduleNoticeOfLeases();
}