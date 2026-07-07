namespace RetailMedia.Web.Tenancy;

public sealed class TenantContext
{
    public string TenantId { get; init; } = string.Empty;
    public string TenantName { get; init; } = string.Empty;
    public Guid CorrelationId { get; init; }
    public string UserId { get; init; } = string.Empty;
}
