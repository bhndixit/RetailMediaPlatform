using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using RetailMedia.Aggregator.Handlers;
using RetailMedia.Collector.Domain;
using RetailMedia.Collector.Infrastructure;
using RetailMedia.Demo.Auth;
using RetailMedia.Infrastructure.Extensions;
using RetailMedia.Messaging.Abstractions;
using RetailMedia.Messaging.Events;
using RetailMedia.Processor.Handlers;
using RetailMedia.Web.Middleware;
using RetailMedia.Web.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {TenantId,-15} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<TenantOptions>(builder.Configuration.GetSection(TenantOptions.Section));

// Shared infrastructure — all services use the SAME singleton bus and analytics store.
builder.Services.AddInfrastructure();

// Collector dependencies
builder.Services.AddSingleton<ICustomerEventRepository, InMemoryCustomerEventRepository>();

// Processor and Aggregator handlers registered so the shared event bus can resolve them.
builder.Services.AddSingleton<CustomerEventEnrichmentHandler>();
builder.Services.AddSingleton<CampaignMetricsAggregationHandler>();

// MediatR scans all service assemblies so every command and query handler is discovered.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<RetailMedia.Collector.Api.Controllers.EventsController>();
    cfg.RegisterServicesFromAssemblyContaining<RetailMedia.Insights.Api.Controllers.CampaignInsightsController>();
});

builder.Services.AddValidatorsFromAssemblyContaining<RetailMedia.Collector.Api.Controllers.EventsController>();

// Controllers from both Collector and Insights are added here.
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(RetailMedia.Collector.Api.Controllers.EventsController).Assembly)
    .AddApplicationPart(typeof(RetailMedia.Insights.Api.Controllers.CampaignInsightsController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Retail Media Intelligence Platform — Demo",
        Version = "v1",
        Description = """
            End-to-end demo host. All services run in a single process sharing one event bus and one analytics store.

            **How to use:**
            1. Select a `X-Tenant-Id` value from the dropdown on each endpoint.
            2. POST to `/api/v1/events` to record a customer interaction.
            3. GET `/api/v1/campaigns/{campaignId}/clicks` (or impressions / clickToBasket) to see the aggregated result.

            No JWT token is required in this demo environment.
            """
    });

    options.OperationFilter<RetailMedia.Demo.Swagger.TenantHeaderOperationFilter>();
});

// Demo auth — every request is automatically authenticated.
// In production: replace "Demo" scheme with JwtBearer.
builder.Services
    .AddAuthentication("Demo")
    .AddScheme<AuthenticationSchemeOptions, DemoAuthHandler>("Demo", null);

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Wire up the event subscriptions so the shared bus knows which handlers to call.
// In production each service subscribes independently on startup.
var eventBus = app.Services.GetRequiredService<IEventBus>();
eventBus.Subscribe<CustomerEventRecorded, CustomerEventEnrichmentHandler>();
eventBus.Subscribe<CustomerEventProcessed, CampaignMetricsAggregationHandler>();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<CorrelationMiddleware>();

// TenantMiddleware only applies to API routes.
// Swagger, health checks, and static assets bypass it.
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    pipeline => pipeline.UseMiddleware<TenantMiddleware>());

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Retail Media Platform v1");
    options.RoutePrefix = string.Empty;
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();
