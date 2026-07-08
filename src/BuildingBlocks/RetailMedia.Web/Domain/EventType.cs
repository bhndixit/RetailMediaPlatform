namespace RetailMedia.Web.Domain;

public static class EventType
{
    public const string Click = "click";
    public const string Impression = "impression";
    public const string Basket = "basket";

    public static readonly IReadOnlyCollection<string> All = [Click, Impression, Basket];
}
