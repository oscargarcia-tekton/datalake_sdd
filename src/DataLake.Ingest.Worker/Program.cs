using DataLake.Application.Abstractions;
using DataLake.Application.Commands;
using DataLake.Classification.Rules;
using DataLake.Infrastructure.Persistence;
using DataLake.Parser.Parsers;
using DataLake.XmlExporter.Templates;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IFileParser, CsvTransactionParser>();
builder.Services.AddScoped<IClassificationEngine, RuleBasedClassificationEngine>();
builder.Services.AddScoped<IXmlExporter, IcomXmlExporter>();

builder.Services.Configure<XmlExportOptions>(builder.Configuration.GetSection("XmlExport"));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<IngestFileCommand>());

builder.Services.AddHostedService<DataLake.Ingest.Worker.IngestWorker>();

var host = builder.Build();
host.Run();
