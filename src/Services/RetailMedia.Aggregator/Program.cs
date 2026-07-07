using RetailMedia.Aggregator.Handlers;
using RetailMedia.Infrastructure.Extensions;
using RetailMedia.Messaging.Abstractions;
using RetailMedia.Messaging.Events;
using RetailMedia.Web.Options;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.Configure<MessagingOptions>(builder.Configuration.GetSection(MessagingOptions.Section));

builder.Services.AddInfrastructure();
builder.Services.AddSingleton<CampaignMetricsAggregationHandler>();

var host = builder.Build();

var eventBus = host.Services.GetRequiredService<IEventBus>();
eventBus.Subscribe<CustomerEventProcessed, CampaignMetricsAggregationHandler>();

await host.RunAsync();
