# 05 — ERROR HANDLING & ENUM BOUNDARY (LOG SERVICE)

## 1. 100% English & Sentence Case
* Error messages must be 100% English sentence case.
* Capitalize only the first letter. Keep technical acronyms uppercase (`ID`, `URL`, `IP`).
* Examples: `"Log entry not found with the specified ID."`, `"Event type is disabled."`, `"Event type not found."`

## 2. SCREAMING_SNAKE_CASE Error Codes
* Error codes must be uppercase with underscores. No `ERR_` prefixes.
* Use `VAL_` prefix for validation errors.
* Examples: `LOG_NOT_FOUND`, `EVENT_TYPE_DISABLED`, `EVENT_TYPE_NOT_FOUND`, `VAL_INVALID_INPUT`, `UNAUTHORIZED`, `FORBIDDEN`.

## 3. RFC 9110 ProblemDetails Structure
* Handled by `GlobalExceptionHandlerMiddleware.cs`: returns `application/problem+json` containing `type`, `title`, `status`, `errorCode`, `detail`, `traceId`.

## 4. Enum Persistence & Boundary Serialization
* In PostgreSQL (`log_db`): 100% stored as `integer` (int32). Prohibit `HasConversion<string>()`.
* Enums: `LogLevel` (`Information = 1`, `Warning = 2`, `Error = 3`, `Critical = 4`).
* REST Outbound: 100% serialized as `PascalCase` strings (`"Information"`, `"Warning"`, `"Error"`).
* REST Inbound: Handled via `PascalCaseJsonStringEnumConverter(allowIntegerValues: false)`.
