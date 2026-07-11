using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace RetailMedia.Web.Middleware;

public sealed class CorrelationMiddleware(RequestDelegate next)
{
    private const string CorrelationHeader = "X-Correlation-Id";

    public const string ContextItemKey = "CorrelationId";
    public const string HeaderName = CorrelationHeader;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault();

        if (!Guid.TryParse(correlationId, out var parsedId))
            parsedId = Guid.NewGuid();

        context.Items[ContextItemKey] = parsedId;
        context.Response.Headers[CorrelationHeader] = parsedId.ToString();

        using (LogContext.PushProperty("CorrelationId", parsedId))
        {
            await next(context);
        }
    }
}
