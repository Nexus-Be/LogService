# 03 — SECURITY HEADERS, INTERNAL API & IDENTITY (LOG SERVICE)

## 1. Controller Segregation
* `Log.Api/Controllers/Public/`: Serves public/admin log exploration (`LogController` at `/api/v1/logs`).
* `Log.Api/Controllers/Internal/`: Serves inter-service log batch ingestion (`InternalLogController` at `/api/v1/internal/logs/batch`).

## 2. Internal Log Batch Ingestion Security
* `POST /api/v1/internal/logs/batch` is strictly protected by `InternalServiceAuthMiddleware` checking header `X-Internal-Service-Key`.
* Never expose `/api/v1/internal/logs/batch` to external users via API Gateway.

## 3. Trusted Identity Extraction
* Querying logs requires administrative authorization via `EnsureAdmin` or `HasRole("admin")`.
