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
}
