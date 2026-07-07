namespace RetailMedia.Insights.Application.Queries;

public sealed record CampaignClicksDto(string CampaignId, string TenantId, long Clicks, DateTime AsOf);
public sealed record CampaignImpressionsDto(string CampaignId, string TenantId, long Impressions, DateTime AsOf);
public sealed record ClickToBasketRatioDto(string CampaignId, string TenantId, decimal Ratio, DateTime AsOf);
