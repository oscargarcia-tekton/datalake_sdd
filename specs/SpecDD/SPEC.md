SPEC — Data Lake to .NET Migration

1. Summary
- Goal: Replace SQL Server stored procedures and Mulesoft flows with a .NET-native solution that ingests transaction files from the Azure Data Lake, classifies transactions, persists movements to a DB, and produces XML batches for the ICOM application.
- Migration strategy: Complete rewrite with phased rollout and parallel validation against existing system.

2. Current flow (as provided)
	- Azure Data Lake stores transaction detail files (CSV/JSON/XML/fixed-width).
	- Stored procedures classify and generate XMLs for batch processing.
	- Windows Task / Control-M triggers batch processing which loads XMLs into ICOM.

	Planned change: remove MuleSoft and related queuing/API layers — replace with a .NET-native pipeline that reads directly from the Data Lake, applies stored-proc classification logic internally, and writes resulting XML batches to the BATCH server.

3. Target architecture (high-level)
- Ingest Service (.NET 8+): reads files from Data Lake (event-driven or scheduled), normalizes records and forwards to Parser.
	- Parser & Normalizer: pluggable parsers for CSV/JSON/XML/fixed-width producing canonical `Transaction` model.
	- Classification Engine: rule-based engine (config-driven) that replaces SP logic; emits `TransactionType` and processing directives.
	- Persistence: EF Core with SQL Server (or managed Azure SQL); schema based on canonical model.
		- CQRS pattern: implement a clear separation between command/write side (command handlers, transactional writes) and read/projection side (denormalized read models) to optimize write performance and downstream queries.
		- Event sourcing: use an append-only event store as the source of truth to provide historical auditability and traceability of transaction processing. Events will be the canonical record of processing steps (ingest, classify, persist, export).
			- Options: EventStoreDB, Azure Event Hubs + durable storage (Blob/Table), or a SQL-backed event store (append-only tables). Choose during discovery based on operational constraints.
			- Projections: implement projection handlers to materialize read models used by downstream reporting and XML export.
			- Snapshotting and retention: implement snapshotting where necessary and define retention/archival policies for events.
	- XML Exporter: generates ICOM-compatible XML files (XSD to be requested) and writes to BATCH server (SFTP or file share).
	- Batch Processor: scheduled worker to group files and mark as complete; can mirror Control-M behaviour.
- Operations: logging (Serilog), metrics (Prometheus/App Insights), tracing (OpenTelemetry).
- Deployment: preferred containerized approach using Docker images.
	- Primary target: Azure App Service (Linux Containers) for straightforward PaaS deployment and low operational overhead.
	- Scale option: AKS or Azure Container Apps can be used when higher orchestration, autoscaling, or advanced networking is required.
	- CI/CD: build multi-stage Docker images, publish to Azure Container Registry (ACR), and deploy to App Service or AKS via pipelines.
	- Note: Windows-hosted services will be considered only if legacy dependencies require them.
	- Autoscaling: enable Azure App Service autoscale rules.
		- Preferred mode: metric-based autoscaling (CPU, memory, HTTP queue length, or custom metrics).
		- Configure scale-out limits, cooldown periods, and a minimum instance count to preserve throughput and warm-up costs.
		- Use preview/perf testing to calibrate rules; integrate autoscale alerts into monitoring.

4. Inputs & outputs
- Inputs: CSV (sample attached), JSON, XML, fixed-width. Fields vary by file type; canonical fields include date, nodeId, currency, amounts (SOD, cash_in, cash_out, etc.), EOD.
- Outputs: ICOM XML files (schema unknown) placed to `/XMLImportTemp/` then moved to `/XMLImportComplete/` when processed.

5. Data model (canonical `Transaction` - draft)
- TransactionId (GUID)
- SourceFileName
- RecordSequence
- TransactionDate (date)
- NodeId (string)
- Currency (string)
- SOD (decimal)
- CashIn (decimal)
- CashOut (decimal)
- ShipIn, ShipOut (decimal)
- EOD (decimal)
- TransactionType (enum)
- ClassificationMetadata (json)
- CreatedAt, ProcessedAt, Status

6. Assumptions
	- No XSD provided — exporter will support schema-driven templating once XSD is available.
	- Stored-proc classification logic is not yet provided; we'll implement a configurable rules engine and mirror SP logic once examples are available.
	- Files are written to Data Lake daily; retention and archival policies handled outside scope.
	- MuleSoft, queueing, and the existing API service will be decommissioned for this pipeline; the .NET solution will directly handle ingestion, classification, persistence, and XML export.

7. Unknowns / Open questions
- Exact XSD/schema expected by ICOM (attach XSD).
- Complete list of stored procedures and classification rules.
- Expected throughput (rows/day) and peak windows.
- Security and network requirements for SFTP/DB access.

8. Migration plan (phases)
- Phase 0 — Discovery: gather SPs, Mulesoft flows, XSDs, and sample files.
- Phase 1 — Implement core Ingest + Parser + Persistence for CSV sample; run parallel write to new DB.
- Phase 2 — Implement Classification Engine and XML Exporter for one transaction type; validate with ICOM in test environment.
- Phase 3 — Incrementally add remaining file formats and transaction types; performance tuning.
- Phase 4 — Cutover: switch Mulesoft -> Ingest service and decommission old flows.

9. Risks
- Missing XSD or SP logic may delay correct XML generation.
- Large throughput may require scale testing and partitioning strategies.

11. Resilience & Failure-Proofing
 - Idempotency: ensure all ingest and processing operations are idempotent using deterministic `ProcessingId` or dedup keys derived from `SourceFileName` + `RecordSequence`.
 - Retries & Backoff: apply retry policies with exponential backoff for transient failures (DB, SFTP, network). Use circuit breakers to prevent cascading failures.
 - Transactional Outbox: implement a transactional outbox pattern to atomically persist state changes and published events, avoiding dual-write inconsistencies between the DB and the event store/exporter.
 - Exactly-once / Deduplication: design for at-least-once delivery with deduplication in consumers; store processed event identifiers in the event store to avoid double-processing.
 - Dead-lettering & Quarantine: route permanently failing records/files to a quarantined store with provenance and error metadata for manual review and reprocessing.
 - Health checks & Liveness/Readiness: expose Kubernetes/App Service health endpoints and readiness probes to drive safe deployments and routing.
 - Graceful Shutdown & Drain: ensure workers complete in-flight processing or persist progress before shutdown to avoid partial writes.
 - Bulkheads & Concurrency Limits: limit concurrency per external dependency (DB, SFTP) using bulkhead patterns to avoid resource exhaustion.
 - Observability & Alerting: instrument tracing (OpenTelemetry), structured logging (Serilog), and metrics (Prometheus/Application Insights). Add alerts for failed exports, DLQ growth, processing lag, and autoscale triggers.
 - Backup & Disaster Recovery: define backup strategy for event store and DB, and test restore procedures periodically.
 - Contract Validation & Schema Checks: validate incoming files against inferred schemas; reject or quarantine malformed records early.
 - Export Reliability: ensure XML export to BATCH server is atomic (write temp file + rename), retried on failure, and acknowledged where possible.
 - Reconciliation & Repair Jobs: implement periodic reconciliation between source files, event store, and exported XMLs to detect and repair gaps.
 - Chaos & Resilience Testing: include chaos experiments and load testing in QA to verify failure modes and recovery behavior.

10. Next steps
- Acquire list of stored procedures and at least one XSD or example XML.
- Confirm desired deployment target and .NET runtime version.
- Run an initial PoC: parse sample CSV, persist to DB, and generate a simple XML file.
