namespace RetailMedia.Collector.Domain;

public interface ICustomerEventRepository
{
    Task SaveAsync(CustomerEvent customerEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerEvent>> FindByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}
