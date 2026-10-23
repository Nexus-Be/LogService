# 04 — MÔ HÌNH THỰC THỂ & CƠ SỞ DỮ LIỆU (LOG SERVICE DATA PERSISTENCE)

---

## 1. MÔ HÌNH THỰC THỂ CHÍNH (DOMAIN ENTITIES)

1. **`LogEntry` (`logs`) — Bất biến (Append-Only)**:
   - `Id`: `Guid` (Primary Key).
   - `OccurredAt`: `DateTimeOffset` (Thời điểm sự kiện diễn ra).
   - `RecordedAt`: `DateTimeOffset` (Thời điểm ghi vào hệ thống log).
   - `ServiceName`: `string` (Tên microservice phát sinh log).
   - `EventType`: `string` (Loại sự kiện, ví dụ: `UserRegistered`, `OrderSubmitted`).
   - `Action`: `string` (Hành động nghiệp vụ).
   - `EntityName`: `string` (Tên thực thể bị tác động, ví dụ: `Order`, `Product`).
   - `EntityId`: `string` (Khóa chính của thực thể).
   - `ActorUserId`: `string` (UUID người thực hiện thao tác).
   - `ActorUsername`: `string`.
   - `ActorRole`: `string`.
   - `CorrelationId`: `string` (Dùng trace xuyên suốt nhiều microservices).
   - `CausationId`: `string`.
   - `MessageId`: `string`.
   - `SchemaVersion`: `int`.
   - `TraceId`: `string` (W3C OpenTelemetry Trace ID).
   - `LogLevel`: `string` (`"INFO"`, `"WARN"`, `"ERROR"`, `"DEBUG"`).
   - `Payload`: `string` (Định dạng native PostgreSQL `jsonb`).

2. **`LogEventType` (`log_event_types`)**:
   - `Code`: `string` (Primary Key, ví dụ: `ProductCreated`, `PaymentCompleted`).
   - `ServiceName`: `string` (Microservice sở hữu sự kiện).
   - `Category`: `string` (Nhóm sự kiện: `Security`, `Order`, `Inventory`...).
   - `Description`: `string` (Mô tả chi tiết).
   - `IsEnabled`: `bool` (Cờ cấu hình bật/tắt ghi nhận log).
   - `CreatedAt`, `UpdatedAt`: `DateTimeOffset`.

---

## 2. QUY CHUẨN POSTGRESQL & INDEXES

- Database: `log_db` tại port `5436`.
- Chuỗi kết nối: `Host=localhost;Port=5436;Database=log_db;Username=postgres;Password=MatkhaucuaTui123@`.
- Bảng chính: `logs`, `log_event_types`.
- Indexes bắt buộc:
  - `ix_logs_occurred_at`: B-Tree Index trên `occurred_at DESC` phục vụ sắp xếp thời gian.
  - `ix_logs_service_name`: B-Tree Index lọc theo service.
  - `ix_logs_event_type`: B-Tree Index lọc theo sự kiện.
  - `ix_logs_actor_user_id`: B-Tree Index lọc theo người thao tác.
  - `ix_logs_correlation_id`: B-Tree Index truy vết chuỗi giao dịch phân tán.
