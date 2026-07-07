using RetailMedia.Infrastructure.Extensions;
using RetailMedia.Messaging.Abstractions;
using RetailMedia.Messaging.Events;
using RetailMedia.Processor.Handlers;
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
builder.Services.AddSingleton<CustomerEventEnrichmentHandler>();

var host = builder.Build();

var eventBus = host.Services.GetRequiredService<IEventBus>();
eventBus.Subscribe<CustomerEventRecorded, CustomerEventEnrichmentHandler>();

await host.RunAsync();
