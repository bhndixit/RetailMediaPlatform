namespace RetailMedia.Web.Options;

public sealed class TenantOptions
{
    public const string Section = "Tenants";
    public List<TenantEntry> Entries { get; init; } = [];
}

public sealed class TenantEntry
{
    public string TenantId { get; init; } = string.Empty;
    public string TenantName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
