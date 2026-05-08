using DataLake.Domain.Abstractions;
using DataLake.Domain.Enums;
using DataLake.Domain.Events;

namespace DataLake.Domain.Entities;

public sealed class Transaction : AggregateRoot
{
    private Transaction() { }

    public Guid TransactionId { get; private set; }
    public string SourceFileName { get; private set; } = default!;
    public int RecordSequence { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public string NodeId { get; private set; } = default!;
    public string Currency { get; private set; } = default!;
    public decimal? Sod { get; private set; }
    public decimal? CashIn { get; private set; }
    public decimal? CashOut { get; private set; }
    public decimal? ShipIn { get; private set; }
    public decimal? ShipOut { get; private set; }
    public decimal? Eod { get; private set; }
    public TransactionType TransactionType { get; private set; }
    public string? ClassificationMetadata { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>
    /// Deterministic processing key for idempotency: derived from source file + record position.
    /// </summary>
    public string ProcessingId => $"{SourceFileName}:{RecordSequence}";

    public static Transaction Create(
        string sourceFileName,
        int recordSequence,
        DateOnly transactionDate,
        string nodeId,
        string currency,
        decimal? sod, decimal? cashIn, decimal? cashOut,
        decimal? shipIn, decimal? shipOut, decimal? eod)
    {
        var tx = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            SourceFileName = sourceFileName,
            RecordSequence = recordSequence,
            TransactionDate = transactionDate,
            NodeId = nodeId,
            Currency = currency,
            Sod = sod,
            CashIn = cashIn,
            CashOut = cashOut,
            ShipIn = shipIn,
            ShipOut = shipOut,
            Eod = eod,
            TransactionType = TransactionType.Unknown,
            Status = TransactionStatus.Received,
            CreatedAt = DateTimeOffset.UtcNow
        };
        tx.RaiseDomainEvent(new TransactionReceivedEvent(tx.TransactionId, sourceFileName, recordSequence));
        return tx;
    }

    public void Classify(TransactionType type, string? metadata)
    {
        TransactionType = type;
        ClassificationMetadata = metadata;
        Status = TransactionStatus.Classified;
        RaiseDomainEvent(new TransactionClassifiedEvent(TransactionId, type));
    }

    public void MarkExported()
    {
        Status = TransactionStatus.Exported;
        ProcessedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new TransactionExportedEvent(TransactionId));
    }

    public void Quarantine(string reason)
    {
        Status = TransactionStatus.Quarantined;
        ClassificationMetadata = reason;
    }
}
