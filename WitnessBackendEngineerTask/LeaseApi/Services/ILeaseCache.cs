using WitnessBackendEngineerTask.Common.Models;

namespace LeaseApi.Services;

public interface ILeaseCache
{
    Task<ParsedScheduleNoticeOfLease?> GetResultAsync(string titleNumber);
    Task<IReadOnlyList<ParsedScheduleNoticeOfLease>> GetAllResultsAsync();
    Task SetResultAsync(ParsedScheduleNoticeOfLease result);
    Task<LeaseProcessingStatus?> GetStatusAsync(string titleNumber);
    Task SetStatusAsync(LeaseProcessingStatus status);
    Task<bool> TryAcquireParseTriggerLockAsync(TimeSpan ttl);
}
