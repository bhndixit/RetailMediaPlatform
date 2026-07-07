using System.Collections.Concurrent;
using RetailMedia.Collector.Domain;

namespace RetailMedia.Collector.Infrastructure;

public sealed class InMemoryCustomerEventRepository : ICustomerEventRepository
{
    private readonly ConcurrentBag<CustomerEvent> _store = [];

    public Task SaveAsync(CustomerEvent customerEvent, CancellationToken cancellationToken = default)
    {
        _store.Add(customerEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CustomerEvent>> FindByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CustomerEvent> result = _store
            .Where(e => e.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult(result);
    }
}
