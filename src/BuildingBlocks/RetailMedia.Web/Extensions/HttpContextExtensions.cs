using Microsoft.AspNetCore.Http;
using RetailMedia.Web.Tenancy;

namespace RetailMedia.Web.Extensions;

public static class HttpContextExtensions
{
    public static TenantContext GetTenantContext(this HttpContext context)
    {
        if (context.Items.TryGetValue(nameof(TenantContext), out var value) && value is TenantContext tenant)
            return tenant;

        throw new InvalidOperationException("TenantContext is not available. Ensure TenantMiddleware is registered.");
    }
}
