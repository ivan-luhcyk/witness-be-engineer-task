using LeaseProcessing.Functions.Models;

namespace LeaseProcessing.Functions.Services;

public interface IHmlrClient
{
    Task<IReadOnlyList<RawScheduleNoticeOfLease>> GetRawSchedulesAsync(CancellationToken cancellationToken);
}
