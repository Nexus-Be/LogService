# 02 — API ROUTING, ENVELOPES & PAGINATION (LOG SERVICE)

## 1. Route Conventions in LogService
* Collections: Lowercase plural nouns (`/api/v1/logs`, `/api/v1/logs/{id}`, `/api/v1/logs/event-types`).
* Prohibit singular nouns (`/api/v1/log`).
* No duplicate route aliases. Action `QueryLogs` must have exactly one official route: `[HttpGet("logs")]`.

## 2. Pure DTO Return & Zero ResultFilter
* Controllers return pure DTOs:
  * `Ok(logEntryDto)`
  * `Ok(eventTypesList)`
  * `Ok(pagedLogs)` (for paginated log queries)
* Zero ResultFilter: Do NOT apply `ResultFilter` or manual envelope factories in `Log.Api`. Gateway automatically wraps raw DTOs in `ApiResponse<T>`.

## 3. Database Pagination Governance
* Database-level pagination: `.Skip((page - 1) * pageSize).Take(pageSize)` and `.CountAsync()`.
* Output: Standard `PagedData<T>` envelope containing `items` and `pagination` metadata.
