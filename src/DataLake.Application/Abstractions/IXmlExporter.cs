using DataLake.Domain.Entities;

namespace DataLake.Application.Abstractions;

public interface IXmlExporter
{
    Task ExportAsync(IReadOnlyList<Transaction> transactions, CancellationToken ct = default);
}
