using Microsoft.Extensions.Options;
using RetailMedia.Web.Options;
using RetailMedia.Web.Tenancy;

namespace RetailMedia.Infrastructure.Tenancy;

internal sealed class ConfigurationTenantProvider(IOptions<TenantOptions> options) : ITenantProvider
{
    private readonly TenantOptions _options = options.Value;

    public Task<TenantContext?> ResolveAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var entry = _options.Entries.Find(e =>
            e.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase) && e.IsActive);

        if (entry is null)
            return Task.FromResult<TenantContext?>(null);

        var context = new TenantContext
        {
            TenantId = entry.TenantId,
            TenantName = entry.TenantName,
            CorrelationId = Guid.NewGuid()
        };

        return Task.FromResult<TenantContext?>(context);
    }
}
