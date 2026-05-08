using DataLake.Domain.Abstractions;

namespace DataLake.Domain.Events;

public sealed record TransactionExportedEvent(Guid TransactionId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
