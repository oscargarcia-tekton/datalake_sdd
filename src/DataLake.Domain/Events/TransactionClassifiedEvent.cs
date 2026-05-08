using DataLake.Domain.Abstractions;
using DataLake.Domain.Enums;

namespace DataLake.Domain.Events;

public sealed record TransactionClassifiedEvent(
    Guid TransactionId,
    TransactionType TransactionType) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
