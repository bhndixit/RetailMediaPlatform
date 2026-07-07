namespace RetailMedia.Web.Options;

public sealed class MessagingOptions
{
    public const string Section = "Messaging";
    public int ChannelCapacity { get; init; } = 1000;
}
