# DataLake SDD — .NET Migration

Migration of the Azure Data Lake ingestion pipeline from SQL Server stored procedures + MuleSoft to a .NET 8 native solution.

## Overview

The pipeline reads transaction files from Azure Data Lake, classifies transactions, persists movements to SQL Server, and produces XML batches for the ICOM application. MuleSoft and the existing SP-based pipeline are decommissioned as part of this migration.

## Architecture

```
Azure Data Lake
      │
      ▼
┌─────────────────┐
│  Ingest Worker  │  Polls/event-driven file pickup (managed identity)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│     Parser      │  Pluggable: CSV · JSON · XML · fixed-width → canonical Transaction
└────────┬────────┘
         │
         ▼
┌─────────────────────┐
│ Classification Engine│  Config-driven rules replacing stored-proc logic
└────────┬────────────┘
         │
         ▼
┌─────────────────┐     ┌──────────────┐
│  Persistence    │────▶│  Outbox      │  Transactional outbox (atomic DB + event write)
│  (EF Core /     │     │  Messages    │
│   Azure SQL)    │     └──────────────┘
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  XML Exporter   │  Generates ICOM XML → /XMLImportTemp/ then renames → /XMLImportComplete/
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Batch Processor │  Groups files, manages lifecycle (mirrors Control-M)
└─────────────────┘
```

## Project Structure

```
src/
├── DataLake.Domain            # Transaction aggregate, domain events, enums
├── DataLake.Application       # CQRS commands/handlers, port interfaces
├── DataLake.Infrastructure    # EF Core DbContext, repositories, outbox
├── DataLake.Parser            # CSV parser (CsvHelper); pluggable for other formats
├── DataLake.Classification    # Rule-based classification engine
├── DataLake.XmlExporter       # ICOM XML generation (schema-driven once XSD is received)
├── DataLake.Ingest.Worker     # .NET Worker Service — file ingestion entry point
└── DataLake.BatchProcessor    # .NET Worker Service — batch grouping and lifecycle
tests/
├── DataLake.Domain.Tests
├── DataLake.Application.Tests
└── DataLake.Integration.Tests  # Testcontainers (real SQL Server)
docker/
├── Dockerfile.ingest
├── Dockerfile.batch
└── docker-compose.yml          # Local dev: SQL Server + both workers
```

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 8 |
| ORM | EF Core + SQL Server (Azure SQL) |
| Messaging pattern | MediatR (CQRS) |
| Resilience | Polly (retries, circuit breakers, bulkheads) |
| Logging | Serilog → Console + Azure Application Insights |
| Tracing | OpenTelemetry |
| CSV parsing | CsvHelper |
| Deployment | Docker → ACR → Azure App Service (Linux Containers) |
| CI/CD | GitHub Actions |

## Getting Started

### Prerequisites

- .NET 8 SDK
- Docker Desktop
- SQL Server (or use the compose stack below)

### Run locally with Docker Compose

```bash
docker compose -f docker/docker-compose.yml up
```

This starts SQL Server, the Ingest Worker, and the Batch Processor. Drop CSV files into the `data-incoming` volume to trigger ingestion.

### Build

```bash
dotnet build DataLake.slnx -c Release
```

### Test

```bash
dotnet test DataLake.slnx
```

Run a single test project:

```bash
dotnet test tests/DataLake.Domain.Tests
```

### EF Core migrations

```bash
dotnet ef migrations add <MigrationName> \
  --project src/DataLake.Infrastructure \
  --startup-project src/DataLake.Ingest.Worker

dotnet ef database update \
  --project src/DataLake.Infrastructure \
  --startup-project src/DataLake.Ingest.Worker
```

## Configuration

Key settings in `appsettings.json` (override via environment variables in production):

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Ingest:WatchPath` | Directory polled for incoming files |
| `XmlExport:ImportTempPath` | Temp write path for XML batches |
| `XmlExport:ImportCompletePath` | Final path after atomic rename |

Secrets (DB password, SFTP credentials, Azure identity) are injected via Azure Key Vault / environment variables — never stored in config files.

## Resilience Patterns

- **Idempotency** — `ProcessingId = SourceFileName:RecordSequence` prevents double-processing on retry
- **Transactional outbox** — DB state and domain events written atomically
- **Polly** — exponential backoff + circuit breakers on all external calls
- **Dead-letter / quarantine** — failing records persisted with provenance for manual review
- **Atomic XML export** — write to temp file, rename to final path; never direct write

## Migration Phases

| Phase | Scope |
|---|---|
| 0 — Discovery | Gather SPs, MuleSoft flows, XSDs, sample files |
| 1 — Core PoC | CSV ingest + persistence; parallel write alongside legacy |
| 2 — Classification + Export | Rules engine + XML exporter for one transaction type |
| 3 — Remaining formats | JSON, XML, fixed-width; performance tuning |
| 4 — Cutover | Switch MuleSoft → Ingest Worker; decommission legacy |

## Open Items

- ICOM XSD / example XML (required before XML Exporter can be schema-validated)
- Complete stored-procedure list and classification rule descriptions
- Expected throughput (rows/day, peak rows/hour)
- SFTP vs. file-share delivery for BATCH server

## CI/CD

GitHub Actions (`.github/workflows/ci.yml`):
- **PR / feature branches** — restore, build, test
- **`main`** — build + test + push Docker images to Azure Container Registry

Required repository secrets: `ACR_REGISTRY`, `ACR_USERNAME`, `ACR_PASSWORD`.
