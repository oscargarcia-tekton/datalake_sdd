using DataLake.Domain.Entities;

namespace DataLake.Application.Abstractions;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task<Transaction?> FindByProcessingIdAsync(string processingId, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetPendingExportAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
