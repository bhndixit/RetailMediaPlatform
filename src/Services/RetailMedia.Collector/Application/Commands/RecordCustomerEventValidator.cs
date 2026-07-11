using FluentValidation;
using RetailMedia.Web.Domain;

namespace RetailMedia.Collector.Application.Commands;

internal sealed class RecordCustomerEventValidator : AbstractValidator<RecordCustomerEventCommand>
{
    public RecordCustomerEventValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.CampaignId).NotEmpty().MaximumLength(256);
        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(256);
        RuleFor(x => x.EventType)
            .NotEmpty()
            .Must(t => EventType.All.Contains(t.ToLowerInvariant()))
            .WithMessage($"EventType must be one of: {string.Join(", ", EventType.All)}.");
    }
}
