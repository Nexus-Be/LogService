# LOG SERVICE SPEC KIT — CHỈ DẪN KỸ THUẬT CHO AI AGENT & DEVELOPER
> **Trạng thái triển khai (2026-10-24):** ⚠️ Một phần — 4/4 rpc gRPC ✅, OTel ✅, 9 bảng DB ✅; nhưng REST batch ❌, consumer không publisher ⚠️, 0/5 index ❌, 0 test project ❌.
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
Tài liệu này là **Bộ Chỉ Dẫn & Đặc Tả Kỹ Thuật (Spec Kit)** cho repository **LogService** thuộc hệ sinh thái Microservices **Nexus**. Giúp lập trình viên và các AI Coding Agent (Cursor, Claude, Copilot, DeepSeek...) nắm bắt bối cảnh quản lý nhật ký hệ thống tập trung (Centralized Logging), danh mục loại sự kiện log và **loại bỏ hoàn toàn nguy cơ ảo giác (zero hallucination)**.
---
## 1. MỤC LỤC BỘ ĐẶC TẢ LOG SERVICE (SPECS/)
| File Đặc Tả | Trọng Tâm Nội Dung | Trạng thái |
|:---|:---|:---:|
| [00_AGENT_MISSION_GUARDRAILS.md](00_AGENT_MISSION_GUARDRAILS.md) | Bounded Context, Port `8004`, DB `log_db` (`5436`), tính bất biến của log (Append-Only). | ⚠️ |
| [01_CLEAN_ARCHITECTURE_LAYERS.md](01_CLEAN_ARCHITECTURE_LAYERS.md) | Cấu trúc 4 tầng Clean Architecture (`.Api`, `.Application`, `.Domain`, `.Infrastructure`), luồng phụ thuộc. | ⚠️ |
| [02_API_GRPC_CONTRACTS.md](02_API_GRPC_CONTRACTS.md) | Hợp đồng gRPC `LogApi` trong `shop.proto`, tra cứu logs, bật/tắt event type. | ✅ 4/4 rpc |
| [03_EVENT_MESSAGING.md](03_EVENT_MESSAGING.md) | Người lắng nghe toàn sàn qua RabbitMQ + MassTransit **8.3.0** để ghi nhận system logs. | ⚠️ có consumer, 0 publisher |
| [04_DATA_PERSISTENCE.md](04_DATA_PERSISTENCE.md) | Mô hình thực thể `LogEntry` (Append-Only), `LogEventType`, DbContext EF Core 8 (No Repository). | ⚠️ 9 bảng ✅, 0/5 index ❌ |
| [05_DEV_VERIFICATION_CHECKLIST.md](05_DEV_VERIFICATION_CHECKLIST.md) | Lệnh biên dịch, kiểm thử tự động, checklist kiểm tra trước khi commit. | ❌ không có test để chạy |
---
## 2. THÔNG SỐ VẬN HÀNH CHÍNH
- **Ngôn ngữ & Runtime**: .NET 8 (C# 12) — tất cả 4 `.csproj` đều `net8.0`.
- **Cổng gRPC Server**:
  - Theo Docker: **`8004`** ✅ (`Dockerfile:3-4`, `docker-compose.yml:37`).
  - Theo chạy local (`dotnet run`): **`5280` (http) / `7103` (https)** ⚠️ (`Log.Api/Properties/launchSettings.json:17,27`) — **lệch spec `8004`**. Cần `ASPNETCORE_URLS=http://localhost:8004` hoặc `--no-launch-profile` để khớp.
  - Không có cổng HTTP REST nghiệp vụ: chỉ 2 minimal endpoint `/` và `/health` (`Log.Api/Program.cs:66-67`).
- **Cơ Sở Dữ Liệu**: PostgreSQL (Port `5436`, Database: `log_db`, Mật khẩu: `MatkhaucuaTui123@` trong `appsettings.json`; bản mẫu `appsettings.example.json` để placeholder `<REPLACE_WITH_DB_PASSWORD>`).
- **Message Broker**: RabbitMQ (Port `5672`, Virtual Host: `/`) — cấu hình đọc từ section `RabbitMq` (`InfrastructureDependencyInjection.cs:44-53`).
- **Nguyên Tắc Bất Biến (Immutability)**: Bảng `Logs` chỉ được phép `INSERT` và `SELECT`. Tuyệt đối cấm thao tác `UPDATE` hoặc `DELETE` — ✅ code hiện tại không có đường nào làm việc đó.
- **Quy mô**: 40 file `.cs` / 1.359 LOC (không tính `Migrations/`); `Log.sln` 4 project; **0 test project**.
| Tham số | Giá trị | Trạng thái | Bằng chứng |
|---|---|---|---|
| HTTP/gRPC port (Docker) | `8004` | ✅ | `Dockerfile:3-4` |
| HTTP/HTTPS port (local) | `5280` / `7103` | ⚠️ lệch spec | `Log.Api/Properties/launchSettings.json:17,27` |
| DB port | `5436` | ✅ | `docker-compose.yml:19`, `Log.Api/appsettings.json:10` |
| DB name | `log_db` | ✅ | `docker-compose.yml:15` |
| Message bus | RabbitMQ `5672` | ✅ | `Log.Api/appsettings.json:12-17` |
| OTLP endpoint | `http://localhost:4317` | ✅ | `Log.Api/appsettings.json:18-20`, `Log.Api/Extensions/ObservabilityExtensions.cs:10` |