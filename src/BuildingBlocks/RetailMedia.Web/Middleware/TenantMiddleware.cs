using Microsoft.AspNetCore.Http;
using RetailMedia.Web.Tenancy;
using Serilog.Context;

namespace RetailMedia.Web.Middleware;

public sealed class TenantMiddleware(RequestDelegate next, ITenantProvider tenantProvider)
{
    private const string TenantHeader = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.Request.Headers[TenantHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Missing X-Tenant-Id header.");
            return;
        }

        var tenant = await tenantProvider.ResolveAsync(tenantId, context.RequestAborted);

        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Tenant not recognised.");
            return;
        }

        context.Items[nameof(TenantContext)] = tenant;

        using (LogContext.PushProperty("TenantId", tenant.TenantId))
        {
            await next(context);
        }
    }
}
