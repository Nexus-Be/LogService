# 01 — PHÂN TẦNG CLEAN ARCHITECTURE & QUY TẮC PHỤ THUỘC (LOG SERVICE)

> **Trạng thái triển khai (2026-10-29):** ✅ ĐÃ CHUẨN HÓA — 4 tầng Clean Architecture chuẩn hóa: Inbound Consumer tại `Log.Application/Consumers/SystemEvents/GlobalLogConsumer.cs`, Consumer DI extension `ConsumerExtensions.cs`, REST API Controller tại `Log.Api/Controllers/LogController.cs`. 100% RESTful HTTP/JSON (CẤM gRPC).
> **Nguồn sự thật:** các file `.csproj` trong repo + code trong `Log.sln` (`Log.Application/Consumers/`, `Log.Infrastructure/InfrastructureDependencyInjection.cs`, `Log.Api/Controllers/LogController.cs`).
> **Cập nhật lần cuối:** 2026-10-29.

Solution `Log.sln` bao gồm 4 project tương ứng với 4 tầng kiến trúc sạch (Clean Architecture):

```
Log.sln
├── Log.Domain/           # 18 file .cs / 252 dòng — Entities (6), Enums (6), Events (ILogEventOccurred)
├── Log.Application/      #  9 file .cs / 430 dòng — CQRS (QueryLogs, GetLog, ListLogEventTypes, SetLogEventTypeEnabled), GlobalLogConsumer
├── Log.Infrastructure/   #  9 file .cs / 349 dòng — LogDbContext, 6 EntityTypeConfiguration, DI, Migrations
└── Log.Api/              #  4 file .cs / 328 dòng — Program.cs, LogGrpcService, Extensions, Middleware
```

---

## 1. NGUYÊN TẮC LUỒNG PHỤ THUỘC (DEPENDENCY RULE)

```
[ Log.Api ] ─────────────┐
                         │
                         ▼
             [ Log.Application ] ──▶ [ Log.Domain ]
                         ▲
                         │
[ Log.Infrastructure ] ──┘
```

- **`Log.Domain`**: Chứa 6 entity (`LogEntry`, `LogEventType`, `CentralAuditLog`, `AuditSyncBatch`, `AppLogFileCatalog`, `SecurityAuditAlert`), 6 enum (`AlertSeverity`, `AlertStatus`, `AppEnvironment`, `AuditSyncStatus`, `CentralAuditAction`, `SecurityRuleType`) và interface `ILogEventOccurred` (`Log.Domain/Events/LogEvents.cs:3`). Không phụ thuộc tầng nào khác trong solution.
- **`Log.Application`**: Chứa interface `IApplicationDbContext` (hoặc dùng trực tiếp `LogDbContext`), các CQRS Queries/Commands (`QueryLogsQuery`, `GetLogQuery`, `ListLogEventTypesQuery`, `SetLogEventTypeEnabledCommand`), và MassTransit Consumer `GlobalLogConsumer` (`Log.Application/Consumers/GlobalLogConsumer.cs:10`).
- **`Log.Infrastructure`**: Chứa `LogDbContext`, 6 cấu hình `EntityTypeConfiguration` Fluent API, ánh xạ cột `Payload` sang kiểu native `jsonb` của PostgreSQL, đăng ký MassTransit + EF Outbox (`Log.Infrastructure/InfrastructureDependencyInjection.cs:34,36-40`), và migration `20261024103313_InitialLogSchema`.
- **`Log.Api`**: Điểm khởi động gRPC Server, đăng ký `app.MapGrpcService<LogGrpcService>()` (`Log.Api/Program.cs:47`), tích hợp OpenTelemetry (`Log.Api/Extensions/ObservabilityExtensions.cs`), Swagger và gRPC reflection.

### 1.1. Trạng thái từng tầng

| Tầng | Trạng thái | Bằng chứng | Ghi chú |
|------|------------|------------|---------|
| `Log.Domain` | ✅ CÓ | `Log.Domain/Log.Domain.csproj`; 18 file `.cs` | ⚠️ Không zero-dependency: `MediatR.Contracts 2.0.1` (`Log.Domain/Log.Domain.csproj:10`) |
| `Log.Application` | ✅ CÓ | `Log.Application/Log.Application.csproj`; 9 file `.cs` | ⚠️ Kéo MassTransit `8.3.0`, FluentValidation `11.9.2`, EFCore `8.0.10`, MediatR `12.4.1` |
| `Log.Infrastructure` | ✅ CÓ | `Log.Infrastructure/Log.Infrastructure.csproj`; 9 file `.cs` (+ `Migrations/`) | EF Core `8.0.10`, Npgsql.EFCore.PostgreSQL `8.0.10`, MassTransit.EntityFrameworkCore `8.3.0` |
| `Log.Api` | ✅ CÓ | `Log.Api/Log.Api.csproj`; 4 file `.cs` | Grpc.AspNetCore `2.66.0`, OpenTelemetry `1.18.0`, Swashbuckle `6.6.2` |
| 5. Batching Sink (gom lô) | ❌ CHƯA CÓ | 0 class buffer/queue/batch trong `src/` | Chỉ là đặc tả mục tiêu; `GlobalLogConsumer` hiện ghi từng message một |

---

## 2. QUY TẮC PHỤ THUỘC BẮT BUỘC (DEPENDENCY GUARDRAILS)

1. **`Log.Domain` không được reference bất kỳ project nào khác trong solution.** Được phép dùng package thuần (hiện có `MediatR.Contracts 2.0.1`) nhưng **không** được kéo EF Core, MassTransit, ASP.NET Core vào Domain.
2. **`Log.Application` chỉ được reference `Log.Domain`.** Không được reference `Log.Infrastructure` hay `Log.Api`. Việc truy cập DB phải qua abstraction (`IApplicationDbContext`) do `Log.Infrastructure` hiện thực.
3. **`Log.Infrastructure` chỉ được reference `Log.Domain` và `Log.Application`.** Không được reference `Log.Api`.
4. **`Log.Api` là project duy nhất được reference cả 3 tầng còn lại.** Đây là Composition Root — nơi duy nhất được phép `AddApplication()`, `AddInfrastructure()` và map endpoint/gRPC service.
5. **CẤM Repository Pattern**: Không tạo `ILogRepository`, `IUnitOfWork`, `GenericRepository<T>`. Application layer dùng trực tiếp `DbSet<T>` qua `IApplicationDbContext`.
6. **CẤM Batching Sink sai cách**: Nếu triển khai gom lô ghi log, phải đặt ở `Log.Infrastructure` (hoặc một project hạ tầng riêng), **không** đặt trong `Log.Domain`. Hiện trạng: **chưa có** — không được mô tả như đã có.

### 2.1. Ma trận phụ thuộc project (đã kiểm chứng)

| Project nguồn | Reference tới | Ghi chú |
|---------------|---------------|---------|
| `Log.Domain` | *(không reference project nào)* | Chỉ có `MediatR.Contracts 2.0.1` |
| `Log.Application` | `Log.Domain` | Cộng package: FluentValidation, MassTransit, MediatR, EFCore, MS.Ext.Abstractions |
| `Log.Infrastructure` | `Log.Domain`, `Log.Application` | Cộng package: MassTransit.EntityFrameworkCore, EFCore (Design/Relational/Tools), Npgsql |
| `Log.Api` | `Log.Domain`, `Log.Application`, `Log.Infrastructure` | Cộng package: Grpc.AspNetCore, Grpc.Tools, OpenTelemetry, Swashbuckle |

---

## 3. VỊ TRÍ ĐẶT CODE MỚI (CHỐNG ĐẶT SAI TẦNG)

| Loại code | Đặt tại | Ví dụ trong repo |
|-----------|---------|------------------|
| Entity, Enum, domain event interface | `Log.Domain/Entities`, `Log.Domain/Enums`, `Log.Domain/Events` | `LogEntry.cs`, `CentralAuditAction.cs`, `LogEvents.cs` |
| Query / Command + Handler + Validator | `Log.Application/Features/{Queries,Commands}/<Tên>` | `Features/Queries/QueryLogs/`, `Features/Commands/SetLogEventTypeEnabled/` |
| MassTransit Consumer | `Log.Infrastructure/Consumers` (hoặc `Log.Application/Consumers`) | `GlobalLogConsumer.cs` |
| `DbContext`, `IEntityTypeConfiguration`, DI, Migration | `Log.Infrastructure` | `LogDbContext.cs`, `Persistence/Configurations/`, `InfrastructureDependencyInjection.cs` |
| REST Controller, middleware, extension DI | `Log.Api` | `Controllers/LogController.cs`, `Extensions/ObservabilityExtensions.cs`, `Program.cs` |
| Batching Sink (khi triển khai) | `Log.Infrastructure` | ❌ chưa có |

---

## 4. QUY CHUẨN CẤU TRÚC CHUẨN HÓA (SERVICE BLUEPRINT)
1. **Giao tiếp đồng bộ (RESTful)**:
   - `Log.Api/Controllers/`: Thin Dispatcher Controller, chỉ nhận request, gọi Service/MediatR Interface trong Application, trả về `IActionResult`. Tuyệt đối cấm logic nghiệp vụ, cấm map anonymous object, cấm khai báo class DTO trong file controller.
   - `Log.Application/`: Chứa các Request/Response DTOs dùng chung cho Controller, Query/Command handlers.
2. **Giao tiếp bất đồng bộ (RabbitMQ MassTransit)**:
   - `Log.Application/Contracts/`: Chứa các Integration Events (ví dụ: `ILogEventOccurred`).
   - `Log.Infrastructure/Consumers/`: Chứa các MassTransit Consumer xử lý sự kiện ghi log phân tán.
3. **Quản lý Dependency Injection**:
   - `Log.Application/`: Đăng ký MediatR/Application Services.
   - `Log.Infrastructure/`: Đăng ký `LogDbContext`, MassTransit Bus, Consumers.
4. **Validation**:
   - Input format validation đặt tại Request DTOs qua Data Annotations hoặc FluentValidation.
   - Business validation đặt tại Application Services hoặc Domain Exceptions.

