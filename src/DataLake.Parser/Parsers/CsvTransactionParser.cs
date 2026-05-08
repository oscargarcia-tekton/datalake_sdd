using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using DataLake.Application.Abstractions;
using DataLake.Domain.Entities;

namespace DataLake.Parser.Parsers;

public sealed class CsvTransactionParser : IFileParser
{
    private static readonly string[] SupportedExtensions = [".csv"];

    public bool CanParse(string fileName)
        => SupportedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());

    public async IAsyncEnumerable<Transaction> ParseAsync(
        Stream fileStream,
        string sourceFileName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        int sequence = 0;
        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            sequence++;

            var dateRaw = csv.GetField("fec_dia") ?? string.Empty;
            if (!DateOnly.TryParseExact(dateRaw, "dd/MM/yyyy", out var date))
                date = DateOnly.FromDateTime(DateTime.UtcNow);

            yield return Transaction.Create(
                sourceFileName: sourceFileName,
                recordSequence: sequence,
                transactionDate: date,
                nodeId: csv.GetField("ide_nodo") ?? string.Empty,
                currency: csv.GetField("cod_moneda") ?? string.Empty,
                sod: ParseDecimal(csv, "SOD"),
                cashIn: ParseDecimal(csv, "cash_in"),
                cashOut: ParseDecimal(csv, "cash_out"),
                shipIn: ParseDecimal(csv, "ship_in"),
                shipOut: ParseDecimal(csv, "ship_out"),
                eod: ParseDecimal(csv, "EOD")
            );
        }
    }

    private static decimal? ParseDecimal(CsvReader csv, string field)
    {
        var raw = csv.GetField(field);
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}
