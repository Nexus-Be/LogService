# LOG SERVICE SPEC KIT — CHỈ DẪN KỸ THUẬT CHO AI AGENT & DEVELOPER

Tài liệu này là **Bộ Chỉ Dẫn & Đặc Tả Kỹ Thuật (Spec Kit)** cho repository **LogService** thuộc hệ sinh thái Microservices **Nexus**. Giúp lập trình viên và các AI Coding Agent (Cursor, Claude, Copilot, DeepSeek...) nắm bắt bối cảnh quản lý nhật ký hệ thống tập trung (Centralized Logging), danh mục loại sự kiện log và **loại bỏ hoàn toàn nguy cơ ảo giác (zero hallucination)**.

---

## 1. MỤC LỤC BỘ ĐẶC TẢ LOG SERVICE (SPECS/)

| File Đặc Tả | Trọng Tâm Nội Dung |
|:---|:---|
| [00_AGENT_MISSION_GUARDRAILS.md](00_AGENT_MISSION_GUARDRAILS.md) | Bounded Context, Port `8004`, DB `log_db` (`5435`), tính bất biến của log (Append-Only). |
| [01_CLEAN_ARCHITECTURE_LAYERS.md](01_CLEAN_ARCHITECTURE_LAYERS.md) | Cấu trúc 4 tầng Clean Architecture (`.Api`, `.Application`, `.Domain`, `.Infrastructure`), luồng phụ thuộc. |
| [02_API_GRPC_CONTRACTS.md](02_API_GRPC_CONTRACTS.md) | Hợp đồng gRPC `LogApi` trong `shop.proto`, tra cứu logs, bật/tắt event type. |
| [03_EVENT_MESSAGING.md](03_EVENT_MESSAGING.md) | Người lắng nghe toàn sàn qua RabbitMQ + MassTransit 8.4.1 để ghi nhận system logs. |
| [04_DATA_PERSISTENCE.md](04_DATA_PERSISTENCE.md) | Mô hình thực thể `LogEntry` (Append-Only), `LogEventType`, DbContext EF Core 8 (No Repository). |
| [05_DEV_VERIFICATION_CHECKLIST.md](05_DEV_VERIFICATION_CHECKLIST.md) | Lệnh biên dịch, kiểm thử tự động, checklist kiểm tra trước khi commit. |

---

## 2. THÔNG SỐ VẬN HÀNH CHÍNH

- **Ngôn ngữ & Runtime**: .NET 8 (C# 12).
- **Cổng gRPC Server**: Port `8004` (Không có cổng HTTP REST riêng, mọi REST call đều qua ApiGateway Port `8000`).
- **Cơ Sở Dữ Liệu**: PostgreSQL (Port `5435`, Database: `log_db`).
- **Message Broker**: RabbitMQ (Port `5672`, Virtual Host: `/`).
- **Nguyên Tắc Bất Biến (Immutability)**: Bảng `LogEntry` chỉ được phép `INSERT` và `SELECT`. Tuyệt đối cấm thao tác `UPDATE` hoặc `DELETE`.
