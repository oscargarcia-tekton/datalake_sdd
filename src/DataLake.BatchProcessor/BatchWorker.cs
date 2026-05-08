namespace DataLake.BatchProcessor;

public sealed class BatchWorker(ILogger<BatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Batch processor started");

        while (!ct.IsCancellationRequested)
        {
            // TODO: implement batch grouping and lifecycle (Temp → Complete) once XSD is confirmed
            logger.LogDebug("Batch processor heartbeat");
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }
}
