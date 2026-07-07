using MediatR;
using RetailMedia.Core.Domain;

namespace RetailMedia.Insights.Application.Queries;

public sealed record GetCampaignClicksQuery(string TenantId, string CampaignId)
    : IRequest<Result<CampaignClicksDto>>;

public sealed record GetCampaignImpressionsQuery(string TenantId, string CampaignId)
    : IRequest<Result<CampaignImpressionsDto>>;

public sealed record GetClickToBasketRatioQuery(string TenantId, string CampaignId)
    : IRequest<Result<ClickToBasketRatioDto>>;
