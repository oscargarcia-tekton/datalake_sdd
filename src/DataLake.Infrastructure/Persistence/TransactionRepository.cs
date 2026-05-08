using DataLake.Application.Abstractions;
using DataLake.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataLake.Infrastructure.Persistence;

public sealed class TransactionRepository(AppDbContext db) : ITransactionRepository
{
    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
        => await db.Transactions.AddAsync(transaction, ct);

    public Task<Transaction?> FindByProcessingIdAsync(string processingId, CancellationToken ct = default)
    {
        // processingId = "fileName:recordSequence" — split and query the indexed columns
        var parts = processingId.Split(':', 2);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var seq))
            return Task.FromResult<Transaction?>(null);

        return db.Transactions
            .FirstOrDefaultAsync(t => t.SourceFileName == parts[0] && t.RecordSequence == seq, ct);
    }

    public async Task<IReadOnlyList<Transaction>> GetPendingExportAsync(CancellationToken ct = default)
        => await db.Transactions
            .Where(t => t.Status == Domain.Enums.TransactionStatus.Classified)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
