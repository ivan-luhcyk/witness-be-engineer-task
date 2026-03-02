using WitnessBackendEngineerTask.Common.Models;

namespace LeaseProcessing.Functions.Services;

public interface ILeaseStore
{
    Task SetStatusAsync(LeaseProcessingStatus status);
    Task SetResultAsync(ParsedScheduleNoticeOfLease result);
}
