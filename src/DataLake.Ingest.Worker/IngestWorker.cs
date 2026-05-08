using DataLake.Application.Commands;
using MediatR;

namespace DataLake.Ingest.Worker;

public sealed class IngestWorker(
    IMediator mediator,
    IConfiguration configuration,
    ILogger<IngestWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Ingest worker started");

        var watchPath = configuration["Ingest:WatchPath"]
            ?? throw new InvalidOperationException("Ingest:WatchPath is not configured.");

        while (!ct.IsCancellationRequested)
        {
            foreach (var file in Directory.GetFiles(watchPath, "*.csv"))
            {
                try
                {
                    var result = await mediator.Send(
                        new IngestFileCommand(file, Path.GetFileName(file)), ct);

                    logger.LogInformation(
                        "Ingested {File}: {Ingested} records, {Quarantined} quarantined",
                        file, result.RecordsIngested, result.RecordsQuarantined);

                    File.Move(file, file + ".processed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to ingest {File}", file);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
