using LeaseApi.Models;

namespace LeaseApi.Clients;

public interface IHmlrClient
{
    Task<IReadOnlyList<RawScheduleNoticeOfLease>> GetSchedulesAsync(CancellationToken cancellationToken = default);
}
