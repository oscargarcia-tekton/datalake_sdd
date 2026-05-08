TASKS — migration work items

High-level tasks (mapped to todo list):

1. Discovery
- Gather SP list and Mulesoft flows
- Obtain XSD / example XMLs
- Collect sample files for each supported format

2. Build PoC (CSV)
- Implement Ingest worker to read CSV from Data Lake
- Implement CSV parser to canonical `Transaction`
- Persist to DB using EF Core
- Implement a simple XML exporter (template) and write to local SFTP folder
- Validate structure with stakeholders

2b. CQRS and write/read separation
- Design command model and command handlers for write operations (idempotency, transactional boundaries).
- Implement projection handlers to build denormalized read models from processed transactions.
- Decide on event store vs. projection-only approach during discovery.
- Implement consistency checks and reconciliation tests between write and read models.
- Add unit and integration tests for command handlers and projections.

3. Resilience & Failure-Proofing Tasks
- Design idempotency key strategy and implement deduplication store.
- Implement transactional outbox for atomic DB+event writes.
- Add retry policies and circuit breakers (use Polly) for external calls (DB, SFTP, Data Lake client).
- Implement dead-letter queue / quarantine storage with provenance metadata.
- Add health endpoints, graceful shutdown handling, and liveness/readiness probes.
- Implement atomic export writer (temp file + rename) and acknowledgement handling where available.
- Build monitoring dashboards and alerts for DLQ, processing lag, export failures, and autoscale indicators.
- Add automated reconciliation jobs and repair utilities.
- Add chaos and resilience test plans and runbooks.

3. Classification
- Reverse-engineer SP logic or translate known rules
- Implement rule engine (config-driven)
- Unit tests for classification rules

4. Batch & Scheduling
- Implement batch grouping and lifecycle (Temp → Complete)
- Integrate with existing scheduling (Control-M) or expose readiness endpoints

5. Non-functional & Ops
- Observability (logs/metrics/traces)
- Security (secrets, SFTP credentials, DB access)
- CI/CD and containerization

6. Cutover & Validation
- Parallel-run validation against legacy system
- Data reconciliation tests
- Cutover plan and rollback strategy

7. Documentation & Handover
- Spec Kit artifacts (this folder)
- Runbooks for operations and troubleshooting

Each task will be broken into sub-tasks and estimated once discovery is complete.
