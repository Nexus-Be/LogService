# 01 — CLEAN ARCHITECTURE & BOUNDED CONTEXT (LOG SERVICE)

## 1. Bounded Context & Port Allocation
* Bounded Context: Centralized audit log ingestion, system event query, log event type toggle, centralized observability.
* Service Port: `8004` (HTTP REST API).
* Database: PostgreSQL `log_db` on port `5436`.

## 2. 4-Layer Clean Architecture
* `Log.Domain`: Entities (`LogEntry`, `LogEventType`, `CentralAuditLog`), Enums (`LogLevel`, `EventSeverity`).
* `Log.Application`: Use cases in `Services/{Feature}/`, interface `ILogDbContext`, request/response records.
* `Log.Infrastructure`: `AppDbContext`, EF Core configurations, migrations.
* `Log.Api`: Thin controllers in `Controllers/Public/` and `Controllers/Internal/`, middlewares, extensions, composition root (`Program.cs`).

## 3. Feature Co-Location (Mandatory)
* `Services/Log/`: `ILogService.cs`, `LogService.cs`, `LogRequests.cs`, `LogResponses.cs`, `LogDtos.cs`.

## 4. Thin 1-to-1 Controller Pairing
* `LogController.cs` -> injects `ILogService` (serves `/api/v1/logs`, `/api/v1/logs/event-types`).
* `InternalLogController.cs` -> injects `ILogService` (serves `/api/v1/internal/logs/batch`).

## 5. Absolute Architectural Invariants
* 🛑 **APPEND-ONLY LOGS**: The `Logs` table is strictly append-only. Zero API endpoints or queries may modify (`UPDATE`) or delete (`DELETE`) log records.
* 🛑 Prohibit Repository Pattern over EF Core (inject `ILogDbContext` directly).
* 🛑 Prohibit MediatR, gRPC, and Message Brokers.
