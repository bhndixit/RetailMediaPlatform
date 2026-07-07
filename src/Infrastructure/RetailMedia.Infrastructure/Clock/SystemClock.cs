using RetailMedia.Core.Abstractions;

namespace RetailMedia.Infrastructure.Clock;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
