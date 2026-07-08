namespace RetailMedia.Collector.Domain;

public interface ICustomerEventRepository
{
    Task SaveAsync(CustomerEvent customerEvent, CancellationToken cancellationToken = default);
}
