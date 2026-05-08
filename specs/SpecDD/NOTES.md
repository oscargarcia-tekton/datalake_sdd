NOTES — assumptions & open questions

Assumptions:
- We'll target .NET 8+ unless constrained by environment.
- SQL Server remains the canonical persistence store initially.
- File access to Data Lake will be via managed identity / secure credentials.

Open questions (prioritized):
1. ~~Which deployment target is preferred (AKS, ACI/Container Apps, App Service, Windows host)?~~ **RESOLVED: Azure Cloud confirmed. Specific service (App Service vs AKS vs Container Apps) TBD during discovery.**
2. What is the expected throughput (rows per day / peak hour)?
3. Can you provide stored procedures or at least names and short descriptions?
4. Is SFTP the required mechanism for delivering XMLs to the BATCH server, or is a file share/API used?

4b. Expected throughput and file volumes (rows/day, peak rows/hour, number and average size of files per day) — unknown. Action: gather metrics from production logs, Mulesoft, or infrastructure team.

Add attachments to this folder as they become available.

Resilience note:
- The solution must implement failure-proof patterns: idempotency, transactional outbox, retries with circuit breakers, DLQ/quarantine, health checks, atomic exports, monitoring/alerts, and reconciliation utilities. Prioritize these during design and the Phase 1 PoC.
