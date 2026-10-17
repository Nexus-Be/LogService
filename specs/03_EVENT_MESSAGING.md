# 03 — SỰ KIỆN BẤT ĐỒNG BỘ QUA RABBITMQ (LOG SERVICE EVENT MESSAGING)

LogService là dịch vụ trung tâm lắng nghe toàn bộ các sự kiện thay đổi trạng thái quan trọng trong hệ sinh thái qua RabbitMQ.

---

## 1. CƠ CHẾ LẮNG NGHE SỰ KIỆN (GLOBAL LOG CONSUMER)

1. **Lắng Nghe Qua Interface Chung `ILogEventOccurred`**:
   - Mọi service khi phát sinh sự kiện quan trọng có thể publish trực tiếp `ILogEventOccurred` hoặc các domain events riêng biệt.
   - `GlobalLogConsumer` nhận message $\rightarrow$ Kiểm tra bảng `LogEventType`:
     - Nếu loại sự kiện đang `IsEnabled == true`: Tạo bản ghi `LogEntry` mới và lưu vào DB.
     - Nếu `IsEnabled == false`: Bỏ qua để tiết kiệm tài nguyên.

2. **Các Domain Events Từ Các Microservices Khác**:
   - `UserService`: `UserRegisteredEvent`, `UserRolesChangedEvent`.
   - `CatalogService`: `ProductCreatedEvent`, `StockReservedEvent`, `StockReleasedEvent`.
   - `CommerceService`: `OrderSubmittedEvent`, `OrderStatusChangedEvent`.
   - `AuctionService`: `BidPlacedEvent`, `AuctionEndedEvent`.
   - `FulfillmentService`: `ShipmentCreatedEvent`, `ShipmentStatusUpdatedEvent`.
   - `WalletService`: `PaymentCompletedEvent`, `PaymentFailedEvent`.

---

## 2. BẢO ĐẢM TÍNH TOÀN VẸN VÀ CHỐNG TRÙNG LẶP

- **Inbox Pattern (MassTransit)**:
  - Tự động lưu `MessageId` vào bảng `InboxState`.
  - Nếu RabbitMQ gửi lại tin nhắn cũ do network ack bị trễ, consumer sẽ tự động phát hiện và bỏ qua, không ghi trùng bản ghi log.
