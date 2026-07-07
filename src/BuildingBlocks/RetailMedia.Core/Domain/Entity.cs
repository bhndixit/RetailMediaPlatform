namespace RetailMedia.Core.Domain;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
    public string TenantId { get; protected set; } = string.Empty;
    public DateTime CreatedAt { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() =>
        _domainEvents.Clear();
}
