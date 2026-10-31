# 00 — NHIỆM VỤ CỐT LÕI & RÀO CHẮN CHỐNG ẢO GIÁC (LOG SERVICE)

> **Trạng thái triển khai (2026-10-24):** ⚠️ Một phần — gRPC `4/4` rpc ✅, OpenTelemetry ✅, `9` bảng PostgreSQL ✅, migration `1/1` ✅; đường ghi log ❌ (`POST /api/v1/internal/logs/batch` không tồn tại, `GlobalLogConsumer` có `0` publisher), index hiệu năng `0/5` ❌, test project `0` ❌.
> **Nguồn sự thật:** code trong repo này (`Log.sln`, 4 project, 40 file `.cs` / 1359 dòng, không tính `Migrations/`).
> **Cập nhật lần cuối:** 2026-10-24.

---

## 1. NHIỆM VỤ & RANH GIỚI MIỀN (BOUNDED CONTEXT)

**LogService** chịu trách nhiệm độc quyền về việc thu thập, lưu vết và truy vấn toàn bộ nhật ký hệ thống (centralized logs) và sự kiện nghiệp vụ trong hệ sinh thái Nexus:
- Ghi nhận nhật ký bất biến (`LogEntry`): Lưu vết ai (`ActorUserId`), đã làm gì (`EventType`, `Action`), trên thực thể nào (`EntityName`, `EntityId`), ở service nào (`ServiceName`), vào lúc nào (`OccurredAt`), mức độ log (`LogLevel`), và dữ liệu chi tiết JSON (`Payload`).
- Tra cứu và lọc nâng cao nhật ký (`QueryLogs`): Theo khoảng thời gian (`From`, `To`), tên service, loại sự kiện, người thực hiện, mức độ log (`LogLevel`) hoặc correlationId.
- Quản lý danh mục loại sự kiện log (`LogEventTypes`): Bật hoặc tắt tính năng ghi log của từng loại sự kiện cụ thể (`SetLogEventTypeEnabled`). Nếu một event type bị tắt (`IsEnabled = false`) $\rightarrow$ Bỏ qua không lưu log để tối ưu hiệu năng và dung lượng.
- Quản trị nhật ký kiểm toán tập trung: `CentralAuditLogs`, `AuditSyncBatches`, `SecurityAuditAlerts`, `AppLogFileCatalogs` (6 entity tổng cộng, xem `specs/04`).
- Cung cấp dịch vụ gRPC Server `LogApi` cho ApiGateway.

### 1.1. Trạng thái từng năng lực

| # | Năng lực | Trạng thái | Bằng chứng |
|---|----------|------------|------------|
| 1 | gRPC Server `LogApi` (4 rpc) | ✅ CÓ | `Log.Api/Services/LogGrpcService.cs:22` (`QueryLogs`), `:67` (`GetLog`), `:99` (`ListLogEventTypes`), `:119` (`SetLogEventTypeEnabled`); map `Log.Api/Program.cs:47`; hợp đồng `Log.Api/Contracts/shop.proto:72-77` |
| 2 | OpenTelemetry (traces / metrics / logs + OTLP exporter) | ✅ CÓ | `Log.Api/Extensions/ObservabilityExtensions.cs:8-25`; gọi tại `Log.Api/Program.cs:12`; endpoint OTLP `http://localhost:4317` (`Log.Api/appsettings.json:18-20`) |
| 3 | Swagger / OpenAPI + gRPC reflection | ✅ CÓ | `Log.Api/Program.cs:37-41` (Swagger), `:23` `AddGrpcReflection`, `:41` `MapGrpcReflectionService` |
| 4 | Nhật ký bất biến `LogEntry` (bảng `Logs`, `Payload` = `jsonb`) | ✅ CÓ | Entity `Log.Domain/Entities/LogEntry.cs`; cấu hình `Log.Infrastructure/Persistence/Configurations/LogEntryConfiguration.cs`; migration `Log.Infrastructure/Migrations/20261024103313_InitialLogSchema.cs` |
| 5 | Đường **ghi** log (ghi từ xa vào `Logs`) | ❌ CHƯA CÓ | `Log.Api/Program.cs` chỉ map `GET /health` (`:66`) và `GET /` (`:67`) — **không** có `POST /api/v1/internal/logs/batch`, **không** có rpc ghi log, 0 controller. Xem `specs/02` §3 |
| 6 | Consumer `ILogEventOccurred` (RabbitMQ) | ⚠️ MỘT PHẦN | Consumer có (`Log.Application/Consumers/GlobalLogConsumer.cs:10`, DI tại `Log.Infrastructure/InfrastructureDependencyInjection.cs:34`) nhưng **0 publisher** trong toàn bộ `src/` ⇒ hiện là dead code. Xem `specs/03` |
| 7 | 5 index hiệu năng trên `Logs` | ❌ CHƯA CÓ (`0/5`) | `LogEntryConfiguration` không có `HasIndex`; migration có 8 `CreateIndex` nhưng không cái nào trên `Logs`. Xem `specs/04` §3 |
| 8 | Test tự động | ❌ CHƯA CÓ | 0 test project trong `Log.sln`; CI `.github/workflows/ci-cd.yml` chỉ build/push Docker. Xem `specs/05` |
| 9 | Batching Sink (gom lô ghi log) | ❌ CHƯA CÓ | Không có class buffer/queue/batch nào; consumer hiện ghi từng message một |
| 10 | Thông báo lỗi 100% English (Nguyên tắc 5) | ❌ CHƯA ĐẠT | 5 chuỗi tiếng Việt trả client: `LogGrpcService.cs:70,74,122`; `SetLogEventTypeEnabledCommand.cs:16,41` |

---

## 2. NHỮNG ĐIỀU CẤM KỴ ĐỐI VỚI AI AGENTS (ANTI-HALLUCINATION GUARDRAILS)

1. **TUYỆT ĐỐI KHÔNG SỬA ĐỔI HOẶC XÓA BẢN GHI LOG (APPEND-ONLY)**:
   - Nhật ký mang tính pháp lý, bảo mật và chẩn đoán sự cố. Bảng `Logs` (entity `LogEntry`) chỉ được phép thêm mới (`INSERT`) và đọc (`SELECT`). Tuyệt đối **KHÔNG CÓ API SỬA HOẶC XÓA LOG**.
   - ⚠️ Lưu ý tên thật: bảng là `Logs` (**không phải** `log`) và **không phải** `logs`; DbSet là `Logs`.

2. **TUYỆT ĐỐI KHÔNG DÙNG REPOSITORY PATTERN BỌC NGOÀI EF CORE**:
   - Tuân thủ nguyên tắc kiến trúc hệ thống: Không sử dụng Repository Pattern bọc ngoài EF Core.
   - Sử dụng trực tiếp `LogDbContext` và `DbSet<T>` (hoặc interface `IApplicationDbContext`) trong tầng Application.
   - Không được tạo `ILogRepository`, `UnitOfWork`.

3. **CÔ LẬP TUYỆT ĐỐI DỮ LIỆU & RANH GIỚI MIỀN (DATABASE-PER-SERVICE)**:
   - `LogService` chỉ sở hữu cơ sở dữ liệu `log_db` trên port `5436`.
   - Mọi dữ liệu từ service khác đưa vào log thông qua RabbitMQ message payload (`Payload` dạng jsonb) hoặc gRPC call.

4. **CẤU HÌNH PORT 8004 VÀ DATABASE 5436 LÀ BẤT BIẾN (NGỮ CẢNH DOCKER)**:
   - Docker: container lắng nghe `8004` — `Dockerfile:3-4` (`EXPOSE 8004`, `ASPNETCORE_URLS=http://+:8004`), `docker-compose.yml:37`.
   - ⚠️ Chạy local bằng `dotnet run` sẽ lấy `Log.Api/Properties/launchSettings.json:17,27` → **`http://localhost:5280`** và **`https://localhost:7103`**, *không* phải `8004`. Muốn khớp gateway khi chạy local phải dùng `ASPNETCORE_URLS=http://localhost:8004 dotnet run --project Log.Api --no-launch-profile`.
   - Chuỗi kết nối DB (`Log.Api/appsettings.json:10`) trỏ tới `localhost:5436`, database `log_db`, user `postgres`. Mật khẩu thật nằm trong `appsettings.json`; bản mẫu `appsettings.Example.json` dùng placeholder `<REPLACE_WITH_DB_PASSWORD>` — **không** được chép mật khẩu thật vào tài liệu mới.

5. **TUYỆT ĐỐI KHÔNG TỰ Ý SỬA PROTOBUF MÀ KHÔNG ĐỒNG BỘ GATEWAY**:
   - Hợp đồng thật của service nằm tại `Log.Api/Contracts/shop.proto` (364 dòng, `service LogApi` ở dòng `72-77`), được biên dịch với `GrpcServices="Server"` (`Log.Api/Log.Api.csproj:35`).
   - ⚠️ **Không có** thư mục `Contracts/` ở cấp repo — mọi tài liệu trỏ `Contracts/shop.proto` phải hiểu là `Log.Api/Contracts/shop.proto`.
   - RPC methods và message formats phải khớp 100% với `shop.proto` gốc của ApiGateway.

---

## 3. QUY MÔ MÃ NGUỒN THỰC TẾ (DÙNG ĐỂ CHỐNG ẢO GIÁC)

| Project | Số file `.cs` | Số dòng (không tính `Migrations/`) | Vai trò |
|---------|---------------|-----------------------------------|---------|
| `Log.Domain` | 18 | 252 | Entities, Enums, Events |
| `Log.Application` | 9 | 430 | CQRS Queries/Commands, Consumer, Behaviors |
| `Log.Infrastructure` | 9 | 349 | `LogDbContext`, Configurations, DI, Migrations |
| `Log.Api` | 4 | 328 | `Program.cs`, `LogGrpcService.cs`, Extensions, Middleware |
| **Tổng** | **40** | **1359** | — |

- `Log.sln` gồm đúng **4 project**, **0 test project**.
- **6 entity**: `LogEntry`, `LogEventType`, `CentralAuditLog`, `AuditSyncBatch`, `AppLogFileCatalog`, `SecurityAuditAlert`.
- **6 enum**: `AlertSeverity`, `AlertStatus`, `AppEnvironment`, `AuditSyncStatus`, `CentralAuditAction`, `SecurityRuleType`.
  - ⚠️ **Không tồn tại** enum `LogSeverity` — mức log là `string LogLevel` (`Log.Domain/Entities/LogEntry.cs:23`).
  - ⚠️ **Không tồn tại** enum `AuditAction` — tên thật là `CentralAuditAction`.
- **9 bảng DB**: 6 bảng nghiệp vụ + 3 bảng hạ tầng MassTransit (`InboxState`, `OutboxMessage`, `OutboxState`).
- **1 migration** + 1 snapshot: `20261024103313_InitialLogSchema`.

---

## 4. NHỮNG SỰ THẬT DỄ BỊ BỊA NHẤT (CHECKLIST NÊN ĐỐI CHIẾU)

Khi viết tài liệu hoặc plan cho LogService, các phát biểu sau **đã bị chứng minh là SAI** và không được lặp lại:

| Phát biểu SAI thường gặp | Sự thật |
|--------------------------|---------|
| "Có `POST /api/v1/internal/logs/batch` để ghi log hàng loạt" | ❌ Không tồn tại. Chỉ có `GET /health` và `GET /` (`Log.Api/Program.cs:66-67`) |
| "Có rpc ghi log (WriteLog / IngestLog)" | ❌ Không có. 4 rpc đều là đọc + cấu hình event type |
| "`GlobalLogConsumer` đang ghi log từ các service khác" | ❌ 0 publisher cho `ILogEventOccurred` trong toàn bộ `src/` |
| "Đã có 5 index hiệu năng trên `logs`" | ❌ `0/5` index trên bảng `Logs` |
| "Tên bảng là `logs` / `log_event_types`" | ❌ Tên thật: `Logs`, `LogEventTypes` (PascalCase, không snake_case) |
| "`Log.Domain` zero dependency" | ❌ Phụ thuộc `MediatR.Contracts 2.0.1` (`Log.Domain/Log.Domain.csproj:10`) |
| "Solution tên `LogService.sln`" | ❌ Tên thật `Log.sln` |
| "Có test project / `dotnet test` xanh" | ❌ 0 test project |
| "MassTransit 8.4.1" | ❌ Thực tế `8.3.0` (`Log.Application/Log.Application.csproj:8-15`) |
| "Chạy `dotnet run` là lên port 8004" | ⚠️ Local lên `5280`/`7103`; chỉ Docker là `8004` |
| "Có `.tools/dotnet-ef` để tạo migration" | ❌ `.tools/` không tồn tại. Dùng `dotnet ef` (global tool) + `Microsoft.EntityFrameworkCore.Tools` (`Log.Infrastructure/Log.Infrastructure.csproj:16`) |
| "Thông báo lỗi đã 100% English" | ❌ Còn 5 chuỗi tiếng Việt (`LogGrpcService.cs:70,74,122`; `SetLogEventTypeEnabledCommand.cs:16,41`) |
