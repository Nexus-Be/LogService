# 04 — AUDIT STORAGE & EVENT TYPE GOVERNANCE (LOG SERVICE)

## 1. Role as Central Audit Aggregator
* Each sibling microservice owns its `AuditLogs` table as the transactional source of truth.
* `LogService` is the cross-service aggregation vault that receives optional background batches via internal route `POST /api/v1/internal/logs/batch`.
* Payload stored as `jsonb` in PostgreSQL (`Logs.Payload`).
* Aggregation never replaces the local audit row and never runs inside a business transaction.

## 2. Event Type Dynamic Governance
* `LogEventTypes` allows runtime enable/disable of event types (`IsEnabled = false` -> bypass storage to conserve database I/O).
* Toggling event types (`PUT /api/v1/logs/event-types/{typeId}/toggle`) must itself generate an internal audit log.

## 3. Strict Append-Only Invariant
* Zero `UPDATE` or `DELETE` statements permitted against the `Logs` table.
* Audit retention is immutable.
