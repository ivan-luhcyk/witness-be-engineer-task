using System.Collections.Generic;
using WitnessBackendEngineerTask.Models.DTOs;

namespace WitnessBackendEngineerTask.Interfaces;

public interface IParsedScheduleDataService
{
    public IEnumerable<ParsedScheduleNoticeOfLease> GetParsedScheduleNoticeOfLeases();
}