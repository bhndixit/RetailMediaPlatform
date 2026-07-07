namespace RetailMedia.Web.Tenancy;

public interface ITenantProvider
{
    Task<TenantContext?> ResolveAsync(string tenantId, CancellationToken cancellationToken = default);
}
