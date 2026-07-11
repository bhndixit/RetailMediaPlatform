using RetailMedia.Aggregator.Handlers;
using RetailMedia.Aggregator.Recovery;
using RetailMedia.Infrastructure.Extensions;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddInfrastructure();
builder.Services.AddSingleton<CampaignMetricsAggregationHandler>();

// MetricsRecoveryService owns the event bus subscription lifecycle:
// it subscribes CampaignMetricsAggregationHandler only after recovery is
// fully complete, guaranteeing no live event can race with counter seeding.
builder.Services.AddHostedService<MetricsRecoveryService>();

var host = builder.Build();
await host.RunAsync();
