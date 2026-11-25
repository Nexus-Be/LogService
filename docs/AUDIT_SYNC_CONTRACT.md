# Central audit log, alerts and health

## How records arrive
LogService pulls. Each source service (User, Commerce, Catalog, Auction, Notification, Wallet, Fulfillment) exposes, with the internal service key (`X-Internal-Service-Key`):
- `GET /api/v1/internal/audit-logs/unsynced?limit=` oldest first, with `unsyncedCount`.
- `POST /api/v1/internal/audit-logs/mark-synced` `{ ids }`.

`AuditSyncWorker` (off unless `AuditSync:Enabled=true`) stores each record in `CentralAuditLogs` (once per source id), redacts sensitive values first, then marks it synced at the source. A source that is down is recorded in `AuditSyncBatches` and retried; nothing is lost. The worker refuses to start if the redaction filter is not registered.

`POST /api/v1/logs/batch` now needs the internal service key (401 without a key header, 403 with a wrong one).

## Redaction
Names hidden at any depth: bank name, code, account number and holder, bank account id, verification evidence, password and hash, tokens, secrets, API and private keys, OTP, card number, CVV, authorization, print token. Non-JSON text is stored as a JSON string.

## Admin API (admin and staff read; closing alerts is admin only)
- `GET /api/v1/admin/audit-logs` filters: `serviceName, entityName, entityId, userId, action, traceId, fromDate, toDate`; `GET .../{id}`.
- `GET /api/v1/admin/audit-logs/sync-status`: per service `state` = `Known` (just read, with `unsyncedCount` and `observedAt`), `Unavailable` (was readable before, not now; `lastKnownUnsyncedCount`, `lastError`) or `Unknown` (never read since this service started).
- `GET /api/v1/admin/security-alerts` filters `status, severity, rule`; `PATCH .../{id}` `{ status, note }` (note required to close; 409 `ALERT_ALREADY_CLOSED`).
- `GET /api/v1/admin/system-health`: one entry per service (`Healthy`, `Degraded`, `Unhealthy`), overall `Degraded` when any is not healthy.

## Rules
`BalanceTampering` (manual balance adjustment at or above the threshold, or repeated within a window), `PrivilegeEscalation` (role assigned), `MassDelete` (ten deletes by one user within five minutes). Thresholds are in `AuditSync` options.

## Audit coverage in Fulfillment
Shipment creation, status change (including carrier webhooks) and update, and warehouse create, update and delete, are written to `audit_logs` in the same save as the change. Receiver name, phone, address and print token are left out.
