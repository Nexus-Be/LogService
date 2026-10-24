# 04 — MÔ HÌNH THỰC THỂ & CƠ SỞ DỮ LIỆU (LOG SERVICE DATA PERSISTENCE)

> **Trạng thái triển khai (2026-10-24):** ⚠️ Một phần — 6/6 entity ✅, 6/6 cấu hình Fluent API ✅, 9/9 bảng đã tạo bởi 1 migration ✅; ❌ **0/5 index** trên bảng `Logs`; ❌ chưa có index cho truy vấn theo thời gian/service/event type/actor/correlation.
> **Nguồn sự thật:** `Log.Domain/Entities/*` (6 file), `Log.Infrastructure/Persistence/LogDbContext.cs` (41 dòng), `Log.Infrastructure/Persistence/Configurations/*` (6 file), `Log.Infrastructure/Migrations/20261024103313_InitialLogSchema.cs`, `Log.Infrastructure/Persistence/Seeds/LogSeedData.cs`.
> **Cập nhật lần cuối:** 2026-10-24.

---

## 1. MÔ HÌNH THỰC THỂ CHÍNH (DOMAIN ENTITIES)

### 1.1. `LogEntry` — bảng `Logs` (Bất biến về mặt API, Append-Only)

Kế thừa `BaseEntity<Guid>` (`Log.Domain/Base/`) ⇒ có sẵn `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`.

| # | Property | Kiểu C# | Kiểu PostgreSQL | Ràng buộc | Bằng chứng |
|---|----------|---------|------------------|-----------|------------|
| 1 | `Id` | `Guid` | `uuid` | PK `PK_Logs` | `Log.Domain/Entities/LogEntry.cs:5`; migration `Logs` block |
| 2 | `OccurredAt` | `DateTimeOffset` | `timestamp with time zone` | NOT NULL, mặc định `UtcNow` | `LogEntry.cs:7` |
| 3 | `RecordedAt` | `DateTimeOffset` | `timestamp with time zone` | NOT NULL | `LogEntry.cs:8` |
| 4 | `ServiceName` | `string` | `character varying(100)` | NOT NULL, max 100 | `LogEntry.cs:9`; `LogEntryConfiguration.cs:12` |
| 5 | `EventType` | `string` | `character varying(100)` | NOT NULL, max 100 | `LogEntry.cs:10`; `LogEntryConfiguration.cs:13` |
| 6 | `Action` | `string` | `character varying(100)` | max 100 | `LogEntry.cs:11`; config `:14` |
| 7 | `EntityName` | `string` | `character varying(100)` | max 100 | `LogEntry.cs:12`; config `:15` |
| 8 | `EntityId` | `string` | `character varying(100)` | max 100 | `LogEntry.cs:13`; config `:16` |
| 9 | `ActorUserId` | `string` | `character varying(100)` | max 100 | `LogEntry.cs:14`; config `:17` |
| 10 | `ActorUsername` | `string` | `character varying(100)` | max 100 | `LogEntry.cs:15`; config `:18` |
| 11 | `ActorRole` | `string` | `character varying(50)` | max 50 | `LogEntry.cs:16`; config `:19` |
| 12 | `CorrelationId` | `string` | `character varying(100)` | max 100 | `LogEntry.cs:17`; config `:20` |
| 13 | `CausationId` | `string` | `character varying(100)` | max 100 | `LogEntry.cs:18`; config `:21` |
| 14 | `MessageId` | `string` | `character varying(100)` | max 100 | `LogEntry.cs:19`; config `:22` |
| 15 | `SchemaVersion` | `int` | `integer` | mặc định `1` | `LogEntry.cs:20` |
| 16 | `TraceId` | `string` | `character varying(100)` | max 100 | `LogEntry.cs:21`; config `:23` |
| 17 | `Payload` | `string` | **`jsonb`** | NOT NULL, mặc định `"{}"` | `LogEntry.cs:22`; config `:25` |
| 18 | `LogLevel` | `string` | `character varying(20)` | max 20, mặc định `"INFO"` | `LogEntry.cs:23`; config `:24` |
| 19 | `CreatedAt` | `DateTimeOffset` | `timestamp with time zone` | từ `BaseEntity` | `Log.Domain/Base/BaseEntity.cs` |
| 20 | `UpdatedAt` | `DateTimeOffset` | `timestamp with time zone` | từ `BaseEntity` | `BaseEntity.cs` |
| 21 | `IsDeleted` | `bool` | `boolean` | từ `BaseEntity` | `BaseEntity.cs` |
| 22 | `DeletedAt` | `DateTimeOffset?` | `timestamp with time zone` | nullable | `BaseEntity.cs` |

**Ghi chú quan trọng về "bất biến":**
- ⚠️ Schema **có** cột `IsDeleted` / `DeletedAt` (di sản của `BaseEntity`), tức về mặt **cấu trúc DB** vẫn có thể soft-delete. Tính bất biến hiện được bảo đảm ở **tầng API**: không có rpc/endpoint nào `UPDATE`/`DELETE`, và `LogDbContext` không override `SaveChanges` để chặn.
- ⚠️ `LogLevel` là **`string`**, **không** phải enum. **Không tồn tại** enum `LogSeverity` trong repo.
- ⚠️ `Payload` khai báo `string` nhưng map `jsonb` — ghi chuỗi không phải JSON hợp lệ sẽ làm PostgreSQL báo lỗi runtime.

### 1.2. `LogEventType` — bảng `LogEventTypes`

| # | Property | Kiểu C# | Kiểu PostgreSQL | Ràng buộc | Bằng chứng |
|---|----------|---------|------------------|-----------|------------|
| 1 | `Code` | `string` | `character varying(100)` | **PK** | `Log.Domain/Entities/LogEventType.cs:5`; `LogEventTypeConfiguration.cs` |
| 2 | `ServiceName` | `string` | `character varying(100)` | max 100 | `LogEventType.cs:6` |
| 3 | `Category` | `string` | `character varying(100)` | max 100 | `LogEventType.cs:7` |
| 4 | `Description` | `string` | `character varying(500)` | max 500 | `LogEventType.cs:8` |
| 5 | `IsEnabled` | `bool` | `boolean` | mặc định `true` | `LogEventType.cs:9` |
| 6 | `CreatedAt` | `DateTimeOffset` | `timestamp with time zone` | — | `LogEventType.cs:10` |
| 7 | `UpdatedAt` | `DateTimeOffset` | `timestamp with time zone` | — | `LogEventType.cs:11` |

> `LogEventType` **không** kế thừa `BaseEntity` ⇒ không có `IsDeleted`/`DeletedAt`.

### 1.3. `CentralAuditLog` — bảng `CentralAuditLogs`

| Property | Kiểu PostgreSQL | Ghi chú |
|----------|------------------|---------|
| `Id` | `uuid` (PK) | `BaseEntity<Guid>` |
| `ServiceName` | `character varying(100)` | — |
| `TraceId` | `character varying(100)` | — |
| `UserId` | `character varying(100)` | — |
| `UserName` | `character varying(255)` | — |
| `Action` | `text` | Enum `CentralAuditAction` lưu dạng **string** (`text`) |
| `EntityName` | `character varying(150)` | — |
| `EntityId` | `character varying(150)` | — |
| `OldValues` | **`jsonb`** | Snapshot trước thay đổi |
| `NewValues` | **`jsonb`** | Snapshot sau thay đổi |
| `ChangedColumns` | **`jsonb`** | Danh sách cột thay đổi |
| `IpAddress` | `character varying(50)` | — |
| `UserAgent` | `character varying(500)` | — |
| `SyncedAt` | `timestamp with time zone` | — |
| `CreatedAt`/`UpdatedAt`/`IsDeleted`/`DeletedAt` | — | Từ `BaseEntity` |

⚠️ Tên enum là **`CentralAuditAction`** (không phải `AuditAction`).

### 1.4. `AuditSyncBatch` — bảng `AuditSyncBatches`

`Id` (`uuid` PK), `ServiceName` (varchar 100), `BatchSize` (int), `FirstRecordId` (varchar 100), `LastRecordId` (varchar 100), `Status` (**`text`**, enum `AuditSyncStatus`), `StartedAt`, `CompletedAt` (timestamptz), `ErrorMessage` (varchar 2000), + 4 cột `BaseEntity`.

### 1.5. `AppLogFileCatalog` — bảng `AppLogFileCatalogs`

`Id` (`uuid` PK), `ServiceName` (varchar 100), `Environment` (**`text`**, enum `AppEnvironment`), `LogDate` (timestamptz), `FilePath` (varchar 1000), `FileSizeBytes` (bigint), `Checksum` (varchar 100), `TotalLines`/`ErrorCount`/`WarningCount` (int), `IsActive`/`IsDisabled` (bool), + 4 cột `BaseEntity`.

⚠️ Đây là bảng **duy nhất có index do cấu hình khai báo**: `IX_AppLogFileCatalogs_ServiceName_LogDate` **UNIQUE** trên `(ServiceName, LogDate)` — `Log.Infrastructure/Persistence/Configurations/AppLogFileCatalogConfiguration.cs:16`.

### 1.6. `SecurityAuditAlert` — bảng `SecurityAuditAlerts`

`Id` (`uuid` PK), `AlertTitle` (varchar 255), `Severity` (**`text`**, enum `AlertSeverity`), `ServiceName` (varchar 100), `CentralAuditLogId` (`uuid`, FK → `CentralAuditLogs`), `TriggeredRule` (**`text`**, enum `SecurityRuleType`), `Description` (varchar 2000), `Status` (**`text`**, enum `AlertStatus`), `ResolvedBy` (`uuid`), `ResolvedAt` (timestamptz), `ResolutionNote` (varchar 2000), + 4 cột `BaseEntity`.

⚠️ Enum thật: `AlertSeverity`, `AlertStatus`, `SecurityRuleType`, `AuditSyncStatus`, `AppEnvironment`, `CentralAuditAction`. **Không tồn tại** `AuditAction`, `LogSeverity`.

### 1.7. Trạng thái entity

| Entity | Bảng | Trạng thái | Bằng chứng |
|--------|------|------------|------------|
| `LogEntry` | `Logs` | ✅ CÓ | `Log.Domain/Entities/LogEntry.cs`; `LogEntryConfiguration.cs` |
| `LogEventType` | `LogEventTypes` | ✅ CÓ | `Log.Domain/Entities/LogEventType.cs`; `LogEventTypeConfiguration.cs` |
| `CentralAuditLog` | `CentralAuditLogs` | ✅ CÓ | `Log.Domain/Entities/CentralAuditLog.cs` |
| `AuditSyncBatch` | `AuditSyncBatches` | ✅ CÓ | `Log.Domain/Entities/AuditSyncBatch.cs` |
| `AppLogFileCatalog` | `AppLogFileCatalogs` | ✅ CÓ | `Log.Domain/Entities/AppLogFileCatalog.cs` |
| `SecurityAuditAlert` | `SecurityAuditAlerts` | ✅ CÓ | `Log.Domain/Entities/SecurityAuditAlert.cs` |
| 3 entity hạ tầng MassTransit | `InboxState`, `OutboxMessage`, `OutboxState` | ✅ CÓ | `LogDbContext.cs:30-32`; migration |

---

## 2. QUY CHUẨN POSTGRESQL & DANH SÁCH BẢNG

- Database: **`log_db`** tại port **`5436`** (`docker-compose.yml:19`; `Log.Api/appsettings.json:10`).
- User: `postgres`. Mật khẩu **không** được ghi trong tài liệu; bản mẫu `appsettings.Example.json` dùng placeholder `<REPLACE_WITH_DB_PASSWORD>`.
- Provider: `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.10`, EF Core `8.0.10` (`Log.Infrastructure/Log.Infrastructure.csproj:9-22`), retry 5 lần / delay 10s (`InfrastructureDependencyInjection.cs:23`).
- Auto-migrate lúc khởi động: `Log.Api/Program.cs:50-64` — nếu DB chưa sẵn sàng thì nuốt exception và chỉ log warning.

### 2.1. Danh sách 9 bảng (đã kiểm chứng trong migration)

| # | Bảng | Loại | Khóa chính | Index hiện có | Trạng thái |
|---|------|------|------------|----------------|------------|
| 1 | `Logs` | Nghiệp vụ | `PK_Logs` (`Id` uuid) | **0** | ✅ bảng CÓ / ❌ index CHƯA CÓ |
| 2 | `LogEventTypes` | Nghiệp vụ (danh mục) | `PK_LogEventTypes` (`Code`) | 0 | ✅ CÓ |
| 3 | `CentralAuditLogs` | Nghiệp vụ | `PK_CentralAuditLogs` (`Id`) | 0 | ✅ CÓ |
| 4 | `AuditSyncBatches` | Nghiệp vụ | `PK_AuditSyncBatches` (`Id`) | 0 | ✅ CÓ |
| 5 | `AppLogFileCatalogs` | Nghiệp vụ | `PK_AppLogFileCatalogs` (`Id`) | `IX_AppLogFileCatalogs_ServiceName_LogDate` (UNIQUE) | ✅ CÓ |
| 6 | `SecurityAuditAlerts` | Nghiệp vụ | `PK_SecurityAuditAlerts` (`Id`) | `IX_SecurityAuditAlerts_CentralAuditLogId` | ✅ CÓ |
| 7 | `InboxState` | Hạ tầng MassTransit | `PK_InboxState` (`Id` bigint) | `IX_InboxState_Delivered` | ✅ CÓ |
| 8 | `OutboxMessage` | Hạ tầng MassTransit | `PK_OutboxMessage` (`SequenceNumber` bigint) | `IX_OutboxMessage_EnqueueTime`, `IX_OutboxMessage_ExpirationTime`, `IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber`, `IX_OutboxMessage_OutboxId_SequenceNumber` | ✅ CÓ |
| 9 | `OutboxState` | Hạ tầng MassTransit | `PK_OutboxState` (`OutboxId` uuid) | `IX_OutboxState_Created` | ✅ CÓ |

**Tổng index trong migration: 8.** Trong đó **0 index trên bảng `Logs`**.

### 2.2. Ràng buộc khóa ngoại

| FK | Từ | Tới | Bằng chứng |
|----|-----|-----|------------|
| `FK_SecurityAuditAlerts_CentralAuditLogs_CentralAuditLogId` | `SecurityAuditAlerts.CentralAuditLogId` | `CentralAuditLogs.Id` | migration dòng 206 |
| `FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId` | `OutboxMessage` | `InboxState` | migration dòng 244 |
| `FK_OutboxMessage_OutboxState_OutboxId` | `OutboxMessage` | `OutboxState` | migration dòng 249 |

---

## 3. INDEXES BẮT BUỘC (ĐANG THIẾU — `0/5`)

Tài liệu cũ hứa 5 index trên `Logs`. **Sự thật: chưa có index nào.** `LogEntryConfiguration.cs` (26 dòng) **không có** lời gọi `HasIndex` nào; migration `20261024103313_InitialLogSchema` cũng không tạo index trên `Logs`.

| # | Index theo đặc tả | Trạng thái | Đề xuất tên thật | Căn cứ nhu cầu truy vấn |
|---|-------------------|------------|------------------|--------------------------|
| 1 | `OccurredAt DESC` (sắp xếp + lọc thời gian) | ❌ CHƯA CÓ | `IX_Logs_OccurredAt` | `QueryLogsQuery` luôn `OrderByDescending(x => x.OccurredAt)` (`QueryLogsQuery.cs:68`) |
| 2 | `ServiceName` | ❌ CHƯA CÓ | `IX_Logs_ServiceName_OccurredAt` | Filter `x.ServiceName == ...` (`QueryLogsQuery.cs:38-39`) |
| 3 | `EventType` | ❌ CHƯA CÓ | `IX_Logs_EventType_OccurredAt` | Filter `x.EventType == ...` (`:41-42`) |
| 4 | `ActorUserId` | ❌ CHƯA CÓ | `IX_Logs_ActorUserId_OccurredAt` | Filter `x.ActorUserId == ...` (`:50-51`) |
| 5 | `CorrelationId` | ❌ CHƯA CÓ | `IX_Logs_CorrelationId` | Filter `x.CorrelationId == ...` (`:53-54`) |
| + | `Payload` (GIN cho jsonb) | ❌ CHƯA CÓ (ngoài 5 index đặc tả) | `IX_Logs_Payload_Gin` | Truy vấn lồng trong JSON hiện **chưa** được dùng, chỉ để dành |

**Cách thêm index (đúng quy trình repo):**
1. Sửa `Log.Infrastructure/Persistence/Configurations/LogEntryConfiguration.cs` — thêm `builder.HasIndex(...)`.
2. Tạo migration: `dotnet ef migrations add AddLogsIndexes --project Log.Infrastructure --startup-project Log.Api` (⚠️ **không** dùng `./.tools/dotnet-ef` — thư mục `.tools/` **không tồn tại**; `Microsoft.EntityFrameworkCore.Tools` đã có ở `Log.Infrastructure/Log.Infrastructure.csproj:16`).
3. **Không** sửa tay migration đã phát hành; chỉ thêm migration mới.

---

## 4. SEED DATA

Nguồn: `Log.Infrastructure/Persistence/Seeds/LogSeedData.cs`, gọi qua `LogDbContext.cs:27` (`Seeds.LogSeedData.SeedAll(modelBuilder)`). `SeedDate = 2026-01-01T00:00:00Z`.

### 4.1. `LogEventTypes` — 5 bản ghi

| # | `Code` | `ServiceName` | `Category` | `IsEnabled` | `Description` (nguyên văn) |
|---|--------|---------------|------------|-------------|----------------------------|
| 1 | `USER_REGISTER` | `User` | `Authentication` | `true` | `Đăng ký tài khoản người dùng mới` |
| 2 | `SELLER_APPROVE` | `User` | `Onboarding` | `true` | `Phê duyệt hồ sơ người bán hàng` |
| 3 | `ORDER_PAID` | `Commerce` | `Order` | `true` | `Thanh toán đơn hàng thành công qua ví điện tử` |
| 4 | `AUCTION_BID` | `Auction` | `Auction` | `true` | `Khách hàng đặt giá thầu trong phiên đấu giá` |
| 5 | `SHIPMENT_DELIVERED` | `Fulfillment` | `Shipment` | `true` | `Giao hàng thành công tới người mua` |

⚠️ **5 mô tả seed này là tiếng Việt** và được trả ra client qua `ListLogEventTypes` ⇒ nằm ngoài 5 chuỗi lỗi đã liệt kê ở `specs/02` §1.2 nhưng cũng vi phạm Nguyên tắc 5 (100% English). Cần dịch cùng đợt khi sửa thông báo lỗi.

### 4.2. `CentralAuditLogs` — 2 bản ghi mẫu

| # | `Id` | `ServiceName` | `Action` | Ghi chú |
|---|------|---------------|----------|---------|
| 1 | `90000000-0000-0000-0000-000000000001` | `User` | `CentralAuditAction.Approve` | `TraceId = trace-user-approve-seller-001`, `UserName = admin@nexus.com`, `EntityName = SellerProfile` |
| 2 | *(bản ghi thứ 2 trong `LogSeedData.cs`, cùng khối `HasData`)* | — | — | Xem trực tiếp `LogSeedData.cs` để lấy giá trị chính xác |

Ngoài ra seed dùng 2 Guid cố định cho actor: `adminUserId = aaaaaaaa-1111-1111-1111-111111111111`, `buyerUserId = dddddddd-4444-4444-4444-444444444444`.

---

## 5. TRUY VẤN ĐỌC (SEMANTICS THẬT CỦA `QueryLogs`)

`Log.Application/Features/Queries/QueryLogs/QueryLogsQuery.cs`:

| Hành vi | Giá trị thật | Bằng chứng |
|---------|--------------|------------|
| `Page` mặc định | `1` nếu `<= 0` | `:33` |
| `PageSize` mặc định | `20` nếu `<= 0`; **tối đa `100`** (`Math.Min`) | `:34` |
| Sắp xếp | `OrderByDescending(OccurredAt)` | `:68` |
| Lọc chuỗi | So sánh **bằng tuyệt đối** (`==`) sau `Trim()`, không `LIKE` | `:38-54` |
| Lọc `LogLevel` | `Trim().ToUpperInvariant()` | `:57` |
| Lọc thời gian | `DateTimeOffset.TryParse` — **bỏ qua im lặng** nếu parse thất bại | `:59-63` |
| Đếm tổng | `CountAsync` **trước** khi phân trang | `:65` |
| Soft-delete filter | ⚠️ **Không có** `HasQueryFilter(x => !x.IsDeleted)` trong `LogEntryConfiguration` ⇒ bản ghi `IsDeleted = true` **vẫn** được trả về | `LogEntryConfiguration.cs` (không có `HasQueryFilter`) |

---

## 6. QUY TẮC BẤT BIẾN DỮ LIỆU (GUARDRAILS)

1. **CẤM** thêm API/rpc `UPDATE` hoặc `DELETE` trên `Logs`.
2. **CẤM** đổi tên bảng/cột đã phát hành — chỉ thêm migration additive.
3. **CẤM** query DB của service khác; mọi dữ liệu ngoài vào qua RabbitMQ `Payload` (jsonb) hoặc gRPC.
4. **CẤM** dùng Repository Pattern; truy cập qua `IApplicationDbContext` / `LogDbContext`.
5. **CẤM** đặt mật khẩu DB thật vào `.md`, `.env.example`, `docker-compose.yml` mới — dùng placeholder.
6. Khi thêm entity mới: tạo `IEntityTypeConfiguration<T>` trong `Log.Infrastructure/Persistence/Configurations/`, thêm `DbSet<T>` vào `LogDbContext` (`:14-20`), rồi tạo migration.
