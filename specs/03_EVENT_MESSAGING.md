# 03 — QUY CHUẨN GIAO TIẾP VÀ BÃI BỎ MESSAGE BROKER (LOG SERVICE)

> **Trạng thái triển khai (2026-10-30):** 🛑 ĐÃ BÃI BỎ HOÀN TOÀN RABBITMQ & MASSTRANSIT — Toàn bộ cơ chế Consumer, Outbox/Inbox tables đã được loại bỏ khỏi LogService. Nhật ký hệ thống được gửi trực tiếp hoặc thu thập qua OpenTelemetry và REST endpoint.
> **Nguồn sự thật:** code (`Log.Api/Controllers/LogController.cs`).
> **Cập nhật lần cuối:** 2026-10-30.

---

## 1. NGUYÊN TẮC BÃI BỎ MESSAGE BROKER

1. **Gỡ bỏ hoàn toàn MassTransit**:
   - Không còn package `MassTransit` hay `MassTransit.EntityFrameworkCore` trong dự án.
   - Thư mục `Log.Application/Consumers` và consumer `GlobalLogConsumer` đã bị xóa bỏ.

2. **Phương thức thu thập nhật ký**:
   - Các dịch vụ hệ thống đẩy trace/metrics/logs qua OpenTelemetry OTLP (`ObservabilityExtensions.cs`).
   - Các ứng dụng bên ngoài hoặc API Gateway gửi nhật ký tập trung qua REST API đồng bộ.
