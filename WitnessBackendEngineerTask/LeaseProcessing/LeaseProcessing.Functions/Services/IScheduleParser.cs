using WitnessBackendEngineerTask.Common.Models;

namespace LeaseProcessing.Functions.Services;

public interface IScheduleParser
{
    IReadOnlyList<ParsedScheduleNoticeOfLease> Parse(IReadOnlyList<LeaseProcessing.Functions.Models.RawScheduleNoticeOfLease> rawSchedules);
}
