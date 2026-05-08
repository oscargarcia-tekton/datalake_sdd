# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project context

This is a spec/design-document repository for migrating the current Data Lake ingestion pipeline (SQL Server stored procedures + MuleSoft) to a .NET-native solution. No implementation code exists yet. The authoritative specs are in [specs/SpecDD/](specs/SpecDD/).

## Planned stack

- Runtime: .NET 8+
- ORM: EF Core with SQL Server (Azure SQL)
- Resilience: Polly (retries, circuit breakers, bulkheads)
- Observability: Serilog, OpenTelemetry, Prometheus / Azure Application Insights
- Deployment: Docker → Azure Container Registry → Azure App Service (Linux Containers); AKS or Azure Container Apps as scale option
- CI/CD: multi-stage Docker builds, ACR, pipeline deploy

## Architecture (target)

The pipeline is decomposed into discrete components:

1. **Ingest Service** — reads files from Azure Data Lake (event-driven or scheduled) using managed identity; forwards raw records to the Parser.
2. **Parser & Normalizer** — pluggable parsers for CSV / JSON / XML / fixed-width producing a canonical `Transaction` model. CSV sample uses `fec_dia` (dd/MM/yyyy), `ide_nodo`, `cod_moneda`, `SOD`, `cash_in`, `cash_out`, `ship_in`, `ship_out`, `EOD`; nulls from empty cells.
3. **Classification Engine** — config-driven rules engine replacing stored-proc logic; emits `TransactionType` and processing directives. SP logic not yet available — implement rules engine skeleton first.
4. **Persistence** — CQRS split: command handlers for transactional writes, projection handlers for denormalized read models. Event sourcing with an append-only event store as source of truth (EventStoreDB, Azure Event Hubs + durable storage, or SQL-backed append-only tables — decision deferred to discovery).
5. **XML Exporter** — generates ICOM-compatible XML files; XSD not yet received. Write to `/XMLImportTemp/`, rename to `/XMLImportComplete/` on success. Delivery is SFTP or file share — TBD.
6. **Batch Processor** — scheduled worker mirroring Control-M behavior to group files and manage lifecycle.

## Key design decisions

- **Transactional outbox**: all state changes and published events must be written atomically (DB + event store) to prevent dual-write inconsistencies.
- **Idempotency**: use deterministic `ProcessingId` derived from `SourceFileName` + `RecordSequence` as dedup key throughout.
- **At-least-once with deduplication**: consumers store processed event IDs; no exactly-once guarantees assumed at transport layer.
- **Dead-letter / quarantine**: permanently failing records are routed to a quarantine store with provenance and error metadata.
- **Atomic XML export**: write to temp file then rename — never write directly to the final path.
- **MuleSoft decommission**: the new pipeline replaces MuleSoft entirely; no bridging layer is planned.

## Current status & open questions

Phase 0 (Discovery) is in progress. Blockers before implementation can begin:
- ICOM XSD / example XML output (required for XML Exporter)
- Complete list of stored procedures / classification rules
- Expected throughput (rows/day, peak rows/hour, file count and sizes)
- Specific Azure service selection (App Service vs. AKS / Container Apps) — Azure Cloud confirmed as platform
- SFTP vs. file-share delivery mechanism for BATCH server

See [specs/SpecDD/NOTES.md](specs/SpecDD/NOTES.md) for full assumptions and open questions, and [specs/SpecDD/TASKS.md](specs/SpecDD/TASKS.md) for phased work items.
