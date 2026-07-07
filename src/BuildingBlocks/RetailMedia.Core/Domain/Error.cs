namespace RetailMedia.Core.Domain;

public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NotFound = new("General.NotFound", "The requested resource was not found.");
    public static readonly Error Unauthorized = new("General.Unauthorized", "Access to this resource is not permitted.");
    public static readonly Error Conflict = new("General.Conflict", "The resource already exists.");

    public static Error Validation(string description) => new("General.Validation", description);
}
