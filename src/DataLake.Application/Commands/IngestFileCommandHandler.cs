using DataLake.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DataLake.Application.Commands;

internal sealed class IngestFileCommandHandler(
    IEnumerable<IFileParser> parsers,
    IClassificationEngine classifier,
    ITransactionRepository repository,
    ILogger<IngestFileCommandHandler> logger) : IRequestHandler<IngestFileCommand, IngestFileResult>
{
    public async Task<IngestFileResult> Handle(IngestFileCommand request, CancellationToken ct)
    {
        var parser = parsers.FirstOrDefault(p => p.CanParse(request.FileName))
            ?? throw new InvalidOperationException($"No parser registered for '{request.FileName}'.");

        int ingested = 0, quarantined = 0;

        await using var stream = File.OpenRead(request.FilePath);
        await foreach (var tx in parser.ParseAsync(stream, request.FileName, ct))
        {
            var existing = await repository.FindByProcessingIdAsync(tx.ProcessingId, ct);
            if (existing is not null)
            {
                logger.LogDebug("Skipping duplicate record {ProcessingId}", tx.ProcessingId);
                continue;
            }

            try
            {
                var (type, metadata) = classifier.Classify(tx);
                tx.Classify(type, metadata);
                await repository.AddAsync(tx, ct);
                ingested++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Quarantining record {ProcessingId}", tx.ProcessingId);
                tx.Quarantine(ex.Message);
                await repository.AddAsync(tx, ct);
                quarantined++;
            }
        }

        await repository.SaveChangesAsync(ct);
        return new IngestFileResult(ingested, quarantined);
    }
}
