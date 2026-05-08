namespace DataLake.Domain.Enums;

public enum TransactionStatus
{
    Received,
    Parsed,
    Classified,
    Persisted,
    Exported,
    Quarantined,
    Failed
}
