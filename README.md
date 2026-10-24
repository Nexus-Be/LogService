# LOG SERVICE — ARCHITECTURE & FOLDER STRUCTURE GUIDE
## (CẨM NANG CẤU TRÚC THƯ MỤC & PHÁT TRIỂN DÀNH CHO DEVELOPER VÀ AI AGENT)
> **Trạng thái triển khai (2026-10-24):** ⚠️ Một phần — gRPC **4/4 rpc** đã có (`LogGrpcService.cs`), OTel ✅, migration ✅; nhưng REST chỉ có **2 minimal endpoint** (`/health`, `/`) — **`POST /api/v1/log/batch` ❌ không tồn tại**; consumer `ILogEventOccurred` có nhưng **0 publisher** trên toàn hệ thống; **0/5 index** hứa trong `specs/04`; **0 test project** (4 project trong `Log.sln`).
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
> **Thông tin dịch vụ**:
> - **Service Port**: `8004` (HTTP/1.1 REST & HTTP/2 gRPC) — theo `Dockerfile:3-4` và `docker-compose.yml:37`. **Chạy local bằng `dotnet run` lại lên `5280` (http) / `7103` (https)** theo `Log.Api/Properties/launchSettings.json:17,27` — xem mục 4.
> - **Database**: PostgreSQL (`log_db`), Port máy host: `5436` (`docker-compose.yml:19`, `Log.Api/appsettings.json:10`).
> - **Quy mô code**: 40 file `.cs` / 1.359 LOC (không tính `Migrations/`) — trong đó Application 9 file, Infrastructure 9, Domain 18, Api 4.
> - **Nhiệm vụ chính (đã có)**: Lưu vết nhật ký tập trung (`LogEntry`), danh mục loại sự kiện (`LogEventType`), mô hình audit bảo mật (`CentralAuditLog`, `AuditSyncBatch`, `AppLogFileCatalog`, `SecurityAuditAlert`).
> - **Nhiệm vụ đặc tả, CHƯA có code**: Thu thập nhật ký hàng loạt qua HTTP (`POST /api/v1/log/batch`) — ❌ xem mục 4.
---
## 1. SƠ ĐỒ PHÂN TẦNG VÀ CẤU TRÚC THƯ MỤC CHUẨN
```
src/Services/Log/
│
├── Log.Domain/                                        # 1. TẦNG DOMAIN (POCO, chỉ 1 package ngoài)
│   ├── Base/                                          # BaseEntity<TKey>, IAuditable, ISoftDelete
│   ├── Common/                                        # BaseEntity (alias BaseEntity<Guid>)
│   ├── Entities/                                      # LogEntry, LogEventType, CentralAuditLog,
│   │                                                  #   AuditSyncBatch, AppLogFileCatalog, SecurityAuditAlert (6 entity)
│   ├── Enums/                                         # AlertSeverity, AlertStatus, AppEnvironment,
│   │                                                  #   AuditSyncStatus, CentralAuditAction, SecurityRuleType (6 enum)
│   ├── Events/                                        # ILogEventOccurred (chỉ interface — 0 publisher)
│   └── Exceptions/                                    # DomainException, EntityNotFoundException,
│                                                      #   BusinessRuleValidationException
│
├── Log.Application/                                   # 2. TẦNG APPLICATION (USE CASES & BUSINESS LOGIC)
│   ├── Common/                                        # Behaviors/ValidationBehavior, Interfaces/IApplicationDbContext
│   ├── Consumers/                                     # GlobalLogConsumer : IConsumer<ILogEventOccurred>
│   ├── DTOs/                                          # LogEntryDto, LogPageDto, LogEventTypeDto, LogEventTypeListDto
│   ├── Features/                                      # 4 use case: QueryLogs, GetLog, ListLogEventTypes,
│   │                                                  #   SetLogEventTypeEnabled (+ 1 validator)
│   └── ApplicationDependencyInjection.cs              # Đăng ký AddApplicationServices()
│
├── Log.Infrastructure/                                # 3. TẦNG INFRASTRUCTURE (CÔNG NGHỆ & HẠ TẦNG CỤ THỂ)
│   ├── Persistence/                                   # Quản lý CSDL PostgreSQL & EF Core 8
│   │   ├── LogDbContext.cs                            # Triển khai IApplicationDbContext (6 DbSet)
│   │   ├── Configurations/                            # 6 Fluent API Configurations (*Configuration.cs) — 0 HasIndex
│   │   └── Seeds/                                     # LogSeedData: 5 LogEventType + 2 CentralAuditLog
│   ├── Migrations/                                    # 1 migration: 20261024103313_InitialLogSchema
│   └── InfrastructureDependencyInjection.cs           # AddInfrastructureServices(configuration) + MassTransit + Outbox
│
└── Log.Api/                                           # 4. TẦNG API / HOST (ENTRY POINT & MIDDLEWARE)
    ├── Extensions/ObservabilityExtensions.cs          # AddServiceObservability (OpenTelemetry OTLP)
    ├── Middleware/GlobalExceptionHandlerMiddleware.cs # Bắt ngoại lệ, format RFC 9110 ProblemDetails
    ├── Services/LogGrpcService.cs                     # gRPC Server: 4 rpc
    ├── Contracts/shop.proto                           # Hợp đồng gRPC dùng chung (364 dòng, `service LogApi` tại :72)
    ├── Log.Api.http                                   # ⚠️ còn request mẫu `/weatherforecast` (404)
    └── Program.cs                                     # Khởi động Kestrel, DI App & Infra, MapGrpcService (:47)
```
Bảng đối chiếu cấu trúc với code:
| Thành phần | Số lượng thật | Bằng chứng | Trạng thái |
|---|---:|---|---|
| Project trong `Log.sln` | 4 | `Log.sln:4-11` | ✅ |
| Test project | 0 | `Log.sln` chỉ có 4 project production | ❌ |
| Entity (`Log.Domain/Entities`) | 6 | `LogEntry`, `LogEventType`, `CentralAuditLog`, `AuditSyncBatch`, `AppLogFileCatalog`, `SecurityAuditAlert` | ✅ |
| Enum (`Log.Domain/Enums`) | 6 | `AlertSeverity`, `AlertStatus`, `AppEnvironment`, `AuditSyncStatus`, `CentralAuditAction`, `SecurityRuleType` | ✅ |
| EF Configuration | 6 | `Log.Infrastructure/Persistence/Configurations/*.cs` | ✅ |
| gRPC rpc | 4 | `LogGrpcService.cs:22,67,99,119` | ✅ |
| REST endpoint | 2 | `Program.cs:66-67` | ✅ |
| `POST /api/v1/log/batch` | 0 | không có `MapPost`, không có controller | ❌ |
---
## 2. NGUYÊN TẮC BẤT BIẾN TẠI LOG SERVICE
1. **APPEND-ONLY (CHỈ THÊM, KHÔNG SỬA/XÓA)**: Nhật ký hệ thống chỉ cho phép `INSERT` và `SELECT`. Tuyệt đối cấm viết API sửa đổi hoặc xóa bản ghi log. Code hiện tại đúng: không có rpc/endpoint nào `UPDATE`/`DELETE` `LogEntry` (`LogGrpcService.cs` chỉ có 4 rpc đọc + 1 rpc cấu hình `LogEventType`).
2. **BATCHING INGESTION**: ⚠️ **Mới là đặc tả, chưa triển khai.** Không có endpoint batch, không có `CreateLogBatchCommand`. Đường ghi log khả dụng duy nhất trong code là `GlobalLogConsumer` (MassTransit) — nhưng consumer này **chưa có publisher** (mục 4.2).
3. **GLOBAL MIDDLEWARE**: `GlobalExceptionHandlerMiddleware` bắt mọi ngoại lệ và chuẩn hóa ProblemDetails — ✅ có (`Program.cs:35`, `Middleware/GlobalExceptionHandlerMiddleware.cs`). ⚠️ Lưu ý: `FluentValidation.ValidationException` **không** có nhánh xử lý riêng → rơi vào nhánh mặc định `500 / INTERNAL_SERVER_ERROR` (`GlobalExceptionHandlerMiddleware.cs:58-62`).
---
## 3. CÁCH CHẠY VÀ KIỂM THỬ ĐỘC LẬP SERVICE
```bash
# 1. Bật database của Log Service (Port 5436):
cd src/Services/Log
docker compose up -d log-db
# 2. Tạo & Cập nhật migration:
#    ❌ `./.tools/dotnet-ef` KHÔNG tồn tại trong repo (đã kiểm tra: không có thư mục .tools/).
#    Dùng EF Core CLI toàn cục (`dotnet ef`) — package Microsoft.EntityFrameworkCore.Tools
#    đã được tham chiếu ở Log.Infrastructure/Log.Infrastructure.csproj:16.
dotnet ef migrations add <TenMigration> --project Log.Infrastructure --startup-project Log.Api
dotnet ef database update --project Log.Infrastructure --startup-project Log.Api
# 3. Chạy service (⚠️ theo launchSettings.json sẽ lên 5280/7103, KHÔNG phải 8004):
dotnet run --project Log.Api
#    Muốn đúng port 8004 như Docker/gateway gọi tới:
ASPNETCORE_URLS=http://localhost:8004 dotnet run --project Log.Api --no-launch-profile
# 4. Kiểm tra sống:
curl http://localhost:8004/health   # -> {"status":"Healthy","service":"Log.Api"}
```
Application tự chạy `db.Database.Migrate()` lúc khởi động nếu DB quan hệ sẵn sàng, lỗi thì chỉ log warning (`Program.cs:49-64`).
`Log.Api/Log.Api.http` ⚠️ vẫn còn `GET {{HostAddress}}/weatherforecast/` — endpoint này **không tồn tại** (chỉ có `/` và `/health`), gọi sẽ 404. File mẫu này chưa được cập nhật theo code thật.
---
## 4. CÁC SỰ THẬT ĐÃ SỬA SO VỚI BẢN CŨ CỦA README
### 4.1. Port: Docker `8004` vs local `5280/7103` (bất biến chỉ đúng ở Docker)
| Ngữ cảnh | Port | Bằng chứng | Trạng thái |
|---|---|---|---|
| Docker image / `ASPNETCORE_URLS` | `8004` | `Dockerfile:3-4`, `docker-compose.yml:37` | ✅ |
| `dotnet run` mặc định (F5) | `5280` (http) / `7103` (https) | `Log.Api/Properties/launchSettings.json:17,27` | ⚠️ lệch spec `8004` |
### 4.2. Đường ống log: hai đầu đều đứt
| Đường | Kỳ vọng | Thực tế | Trạng thái |
|---|---|---|---|
| Gateway → `POST /api/v1/log/batch` | HTTP batch ingestion | `Log.Api` chỉ map `/health` và `/` (`Program.cs:66-67`); không có `MapPost`, 0 controller | ❌ 404 |
| Service khác → `ILogEventOccurred` (RabbitMQ) | Event ingestion | `GlobalLogConsumer` đã đăng ký (`InfrastructureDependencyInjection.cs:34`), nhưng grep toàn hệ thống: **0 chỗ publish** `ILogEventOccurred` | ⚠️ consumer là code chết |
Hệ quả: hiện **không có đường nào ghi được `LogEntry` mới** ngoài `LogSeedData` (seed)md` mục LG-01/LG-02.
### 4.3. Tên enum đã sửa
Bản cũ ghi `LogSeverity`, `AuditAction` — **hai tên này không tồn tại**. Tên thật:
| Tên cũ trong doc | Tên thật trong code | Bằng chứng |
|---|---|---|
| `LogSeverity` | **không có enum**; mức log là `string LogLevel` (`"INFO"/"WARN"/"ERROR"/"DEBUG"`) trên `LogEntry` | `Log.Domain/Entities/LogEntry.cs:23` |
| `AuditAction` | `CentralAuditAction` (`Create, Update, Delete, Login, Logout, Approve, Reject, SoftDelete, Export, Other`) | `Log.Domain/Enums/CentralAuditAction.cs:3-15` |
| `AlertStatus` | `AlertStatus` (`Open, Investigating, Resolved, Dismissed`) — đúng | `Log.Domain/Enums/AlertStatus.cs:3-9` |
### 4.4. "Domain ZERO DEPENDENCY" — không còn đúng 100%
`Log.Domain/Log.Domain.csproj:10` có `<PackageReference Include="MediatR.Contracts" Version="2.0.1" />`. Domain **không** tham chiếu project nào khác (vẫn sạch về ProjectReference), nhưng **không phải zero package**. `Log.Application` kéo thêm MassTransit `8.3.0`, `MassTransit.RabbitMQ 8.3.0`, FluentValidation `11.9.2`, MediatR `12.4.1`, EFCore `8.0.10` (`Log.Application/Log.Application.csproj:8-15`).
### 4.5. Index trên bảng `Logs`: 0/5
`LogEntryConfiguration.cs` không có lời gọi `HasIndex` nào; migration `20261024103313_InitialLogSchema` có **8** `CreateIndex` nhưng **0** index trên bảng `Logs` (chỉ có: `AppLogFileCatalogs(ServiceName, LogDate)` unique, `InboxState(Delivered)`, `OutboxMessage` ×4, `OutboxState(Created)`, `SecurityAuditAlerts(CentralAuditLogId)`). Chi tiết: `specs/04_DATA_PERSISTENCE.md`.
### 4.6. 5 chuỗi tiếng Việt trả về client (không phải 3)
| Chuỗi | Vị trí | Đường tới client |
|---|---|---|
| `"ID nhật ký không hợp lệ"` | `Log.Api/Services/LogGrpcService.cs:70` | gRPC `InvalidArgument` |
| `"Không tìm thấy nhật ký tương ứng"` | `Log.Api/Services/LogGrpcService.cs:74` | gRPC `NotFound` |
| `"Code không được để trống"` | `Log.Api/Services/LogGrpcService.cs:122` | gRPC `InvalidArgument` |
| `"Mã sự kiện (Code) không được để trống."` | `Log.Application/Features/Commands/SetLogEventTypeEnabled/SetLogEventTypeEnabledCommand.cs:16` | `ValidationException`; ở Development, gRPC `EnableDetailedErrors` (`Program.cs:21`) đưa message vào `RpcException.Detail` |
| `"Tự động sinh cấu hình cho {Code}"` | `Log.Application/Features/Commands/SetLogEventTypeEnabled/SetLogEventTypeEnabledCommand.cs:41` | Lưu vào `LogEventTypes.Description` → trả về qua `ListLogEventTypes` |
Ngoài ra `GlobalLogConsumer.cs:32,56` còn 2 chuỗi tiếng Việt nhưng chỉ ghi vào **server log**, không trả client. Mã lỗi gợi ý: `LOG_ID_INVALID`, `LOG_NOT_FOUND`, `LOG_CODE_REQUIRED` (xem `specs/00`, Nguyên tắc 5).
### 4.7. Quy mô & migration
- `Log.sln` có 4 project: `Log.Api`, `Log.Domain`, `Log.Application`, `Log.Infrastructure` (`Log.sln:4-11`). Tên solution là `Log.sln` — dùng đúng tên này khi build.
- `Migrations/` có 1 migration `20261024103313_InitialLogSchema` (+ `.Designer.cs`) và `LogDbContextModelSnapshot.cs`.
- 6 bảng domain + 3 bảng hạ tầng MassTransit (`InboxState`, `OutboxMessage`, `OutboxState`) = **9 bảng** trong migration.