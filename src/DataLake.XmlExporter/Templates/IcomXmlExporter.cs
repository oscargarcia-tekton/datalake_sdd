using System.Xml.Linq;
using DataLake.Application.Abstractions;
using DataLake.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataLake.XmlExporter.Templates;

public sealed class IcomXmlExporter(
    IOptions<XmlExportOptions> options,
    ILogger<IcomXmlExporter> logger) : IXmlExporter
{
    private readonly XmlExportOptions _opts = options.Value;

    public async Task ExportAsync(IReadOnlyList<Transaction> transactions, CancellationToken ct = default)
    {
        var batchId = Guid.NewGuid();
        var tempPath = Path.Combine(_opts.ImportTempPath, $"batch_{batchId}.xml");
        var finalPath = Path.Combine(_opts.ImportCompletePath, $"batch_{batchId}.xml");

        var doc = BuildXml(transactions, batchId);

        // Atomic write: temp file then rename — never write directly to final path
        await using (var stream = File.OpenWrite(tempPath))
            await Task.Run(() => doc.Save(stream), ct);

        File.Move(tempPath, finalPath);
        logger.LogInformation("Exported batch {BatchId} with {Count} records to {Path}", batchId, transactions.Count, finalPath);
    }

    private static XDocument BuildXml(IReadOnlyList<Transaction> transactions, Guid batchId)
    {
        // Schema-driven templating will replace this once the ICOM XSD is received.
        return new XDocument(
            new XElement("ICOMBatch",
                new XAttribute("batchId", batchId),
                new XAttribute("exportedAt", DateTimeOffset.UtcNow.ToString("O")),
                transactions.Select(tx => new XElement("Transaction",
                    new XAttribute("id", tx.TransactionId),
                    new XElement("Date", tx.TransactionDate.ToString("yyyy-MM-dd")),
                    new XElement("NodeId", tx.NodeId),
                    new XElement("Currency", tx.Currency),
                    new XElement("SOD", tx.Sod),
                    new XElement("CashIn", tx.CashIn),
                    new XElement("CashOut", tx.CashOut),
                    new XElement("ShipIn", tx.ShipIn),
                    new XElement("ShipOut", tx.ShipOut),
                    new XElement("EOD", tx.Eod),
                    new XElement("Type", tx.TransactionType.ToString())
                ))
            )
        );
    }
}

public sealed class XmlExportOptions
{
    public string ImportTempPath { get; set; } = "/XMLImportTemp";
    public string ImportCompletePath { get; set; } = "/XMLImportComplete";
}
