using MediatR;
using RetailMedia.Core.Domain;

namespace RetailMedia.Collector.Application.Commands;

public sealed record RecordCustomerEventCommand(
    string TenantId,
    Guid CorrelationId,
    string CampaignId,
    string CustomerId,
    string EventType
) : IRequest<Result<Guid>>;
