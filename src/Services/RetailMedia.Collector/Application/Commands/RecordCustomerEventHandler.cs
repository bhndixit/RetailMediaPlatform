using MediatR;
using RetailMedia.Collector.Domain;
using RetailMedia.Core.Domain;
using RetailMedia.Messaging.Abstractions;
using RetailMedia.Messaging.Events;

namespace RetailMedia.Collector.Application.Commands;

internal sealed class RecordCustomerEventHandler(
    ICustomerEventRepository repository,
    IEventBus eventBus)
    : IRequestHandler<RecordCustomerEventCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RecordCustomerEventCommand command, CancellationToken cancellationToken)
    {
        var customerEvent = CustomerEvent.Record(
            command.TenantId,
            command.CampaignId,
            command.CustomerId,
            command.EventType,
            command.CorrelationId);

        await repository.SaveAsync(customerEvent, cancellationToken);

        await eventBus.PublishAsync(new CustomerEventRecorded(
            EventId: Guid.NewGuid(),
            TenantId: command.TenantId,
            CorrelationId: command.CorrelationId,
            OccurredAt: customerEvent.OccurredAt,
            Version: 1,
            CustomerEventId: customerEvent.Id,
            CampaignId: command.CampaignId,
            CustomerId: command.CustomerId,
            EventType: command.EventType
        ), cancellationToken);

        return Result<Guid>.Success(customerEvent.Id);
    }
}
