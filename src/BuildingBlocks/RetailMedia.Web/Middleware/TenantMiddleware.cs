using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RetailMedia.Web.Tenancy;
using Serilog.Context;
using System.Text.Json;

namespace RetailMedia.Web.Middleware;

public sealed class TenantMiddleware(RequestDelegate next, ITenantProvider tenantProvider)
{
    private const string TenantHeader = "X-Tenant-Id";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.Request.Headers[TenantHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                "Missing Tenant", $"The '{TenantHeader}' request header is required.");
            return;
        }

        var tenant = await tenantProvider.ResolveAsync(tenantId, context.RequestAborted);

        if (tenant is null)
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                "Tenant Not Recognised", $"No active tenant found for identifier '{tenantId}'.");
            return;
        }

        // Use the correlation ID already established by CorrelationMiddleware so that
        // every log line and integration event shares one traceable ID for the request.
        var correlationId = context.Items.TryGetValue(CorrelationMiddleware.ContextItemKey, out var raw)
            && raw is Guid parsed
                ? parsed
                : Guid.NewGuid();

        var tenantContext = new TenantContext
        {
            TenantId = tenant.TenantId,
            TenantName = tenant.TenantName,
            CorrelationId = correlationId
        };

        context.Items[nameof(TenantContext)] = tenantContext;

        using (LogContext.PushProperty("TenantId", tenantContext.TenantId))
        {
            await next(context);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
