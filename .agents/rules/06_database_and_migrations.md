# 06 — DATABASE PERSISTENCE & MIGRATIONS (LOG SERVICE)

## 1. Database Isolation
* Dedicated database: `log_db` on host port `5436`.
* Primary key: 100% `Guid` for entities (`Logs`, `LogEventTypes`, `CentralAuditLogs`).
* `Logs.Payload`: configured as PostgreSQL `jsonb` column for efficient indexing and querying.

## 2. Performance Indexes
* Mandatory B-Tree indexes on `Logs`:
  * `OccurredAt` (descending)
  * `ServiceName`
  * `EventType`
  * `ActorUserId`
  * `LogLevel`

## 3. Migration Safety
* No destructive migrations (`DROP TABLE`, `DROP COLUMN`) without explicit authorization.
* Startup safe auto-migration: `db.Database.Migrate()` wrapped in `try-catch` checking `db.Database.IsRelational()`.
