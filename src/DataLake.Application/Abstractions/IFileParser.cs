using DataLake.Domain.Entities;

namespace DataLake.Application.Abstractions;

public interface IFileParser
{
    bool CanParse(string fileName);
    IAsyncEnumerable<Transaction> ParseAsync(Stream fileStream, string sourceFileName, CancellationToken ct = default);
}
