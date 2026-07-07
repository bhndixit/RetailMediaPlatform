using FluentValidation;

namespace RetailMedia.Collector.Application.Commands;

internal sealed class RecordCustomerEventValidator : AbstractValidator<RecordCustomerEventCommand>
{
    public RecordCustomerEventValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.EventType)
            .NotEmpty()
            .Must(t => AllowedEventTypes.Contains(t.ToLowerInvariant()))
            .WithMessage("EventType must be one of: click, impression, basket.");
    }

    private static readonly HashSet<string> AllowedEventTypes = ["click", "impression", "basket"];
}
