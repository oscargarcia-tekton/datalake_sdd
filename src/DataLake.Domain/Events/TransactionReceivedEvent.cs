using DataLake.Domain.Abstractions;

namespace DataLake.Domain.Events;

public sealed record TransactionReceivedEvent(
    Guid TransactionId,
    string SourceFileName,
    int RecordSequence) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
