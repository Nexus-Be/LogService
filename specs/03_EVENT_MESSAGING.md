# 03 — SỰ KIỆN BẤT ĐỒNG BỘ QUA RABBITMQ (LOG SERVICE EVENT MESSAGING)

> **Trạng thái triển khai (2026-10-24):** ⚠️ Một phần — consumer `GlobalLogConsumer` ✅ CÓ và đã đăng ký với MassTransit `8.3.0`; ✅ EF Outbox/Inbox đã cấu hình; ❌ **`0` publisher** cho `ILogEventOccurred` trong toàn bộ `src/` ⇒ đường ghi log này hiện là **dead code**. LogService **không** publish bất kỳ message nào (`0` chỗ dùng `IPublishEndpoint`).
> **Nguồn sự thật:** `Log.Domain/Events/LogEvents.cs` (12 dòng), `Log.Application/Consumers/GlobalLogConsumer.cs` (59 dòng), `Log.Infrastructure/InfrastructureDependencyInjection.cs` (61 dòng).
> **Cập nhật lần cuối:** 2026-10-24.

LogService được thiết kế là dịch vụ trung tâm lắng nghe toàn bộ các sự kiện thay đổi trạng thái quan trọng trong hệ sinh thái qua RabbitMQ. **Hiện tại** phần "lắng nghe" mới có phần nhận (consumer), chưa có phần gửi (publisher) từ bất kỳ service nào.

---

## 1. CƠ CHẾ LẮNG NGHE SỰ KIỆN (GLOBAL LOG CONSUMER)

### 1.1. Hợp đồng message

```csharp
// Log.Domain/Events/LogEvents.cs:3-12
public interface ILogEventOccurred
{
    string ServiceName { get; }
    string EventType { get; }
    string ActorUserId { get; }
    string EntityName { get; }
    string EntityId { get; }
    string DetailsJson { get; }
    string LogLevel { get; }
}
```

⚠️ Đây là **interface**, không phải class/DTO. MassTransit bind consumer theo **kiểu message** (`ILogEventOccurred` với namespace `Log.Domain.Events`), nên bất kỳ service nào muốn publish phải **reference `Log.Domain`** và publish một implementation của interface này. Đây là ràng buộc kiến trúc quan trọng: hợp đồng message hiện **không** tự mô tả được bằng JSON thuần như các integration event thông thường.

### 1.2. Luồng xử lý thực tế

1. **Nhận message** qua `IConsumer<ILogEventOccurred>` (`Log.Application/Consumers/GlobalLogConsumer.cs:10`, hàm `Consume` tại `:21`).
2. **Kiểm tra cấu hình event type** (`:26-28`): truy vấn `LogEventTypes` theo `Code == msg.EventType` với `AsNoTracking()`.
3. **Nếu `config != null && !config.IsEnabled`** → log mức `Debug` và `return`, **không** ghi bản ghi (`:30-34`).
   - ⚠️ Nếu **không tìm thấy** `config` (`config == null`) thì vẫn **ghi log** — hành vi "mặc định cho phép", khác với mô tả "chỉ ghi khi IsEnabled = true" trong tài liệu cũ.
4. **Tạo `LogEntry`** (`:36-51`) và `_context.Logs.Add(entry)` + `SaveChangesAsync` (`:53-54`).
5. **Ghi server log** `Information` (`:56-57`) — chuỗi này **tiếng Việt**, nhưng chỉ vào log server, không trả client.

### 1.3. Trạng thái từng thành phần

| # | Thành phần | Trạng thái | Bằng chứng | Ghi chú |
|---|------------|------------|------------|---------|
| 1 | Message contract `ILogEventOccurred` | ✅ CÓ | `Log.Domain/Events/LogEvents.cs:3` | Là `interface`, 7 property chỉ-đọc |
| 2 | Consumer `GlobalLogConsumer` | ✅ CÓ | `Log.Application/Consumers/GlobalLogConsumer.cs:10,21` | Ghi vào `Logs` qua `IApplicationDbContext` |
| 3 | Đăng ký consumer vào MassTransit | ✅ CÓ | `Log.Infrastructure/InfrastructureDependencyInjection.cs:34` `x.AddConsumer<GlobalLogConsumer>()` | Endpoint name tự sinh (`SetKebabCaseEndpointNameFormatter`, `:32`) |
| 4 | Kết nối RabbitMQ | ✅ CÓ | `Log.Infrastructure/InfrastructureDependencyInjection.cs:42-56` | Host/Port/User/Password đọc từ section `RabbitMq` (`Log.Api/appsettings.json:12-17`), mặc định `localhost:5672`, user `guest` |
| 5 | Publisher `ILogEventOccurred` (bất kỳ service nào) | ❌ **CHƯA CÓ (0)** | `grep -rn "ILogEventOccurred" src/` → chỉ 3 hit trong repo Log (khai báo + dùng trong consumer); **không** có ngoài repo Log | Toàn bộ đường ghi log qua RabbitMQ hiện **không hoạt động** |
| 6 | LogService tự publish message | ❌ CHƯA CÓ | `0` chỗ dùng `IPublishEndpoint` / `IBus` trong repo | `UseBusOutbox()` đã bật nhưng chưa có publisher nào dùng |
| 7 | Consumer cho domain event riêng của service khác (`UserRegisteredEvent`, `OrderSubmittedEvent`, …) | ❌ **CHƯA CÓ** | Không có `IConsumer<...>` nào khác ngoài `GlobalLogConsumer` | Xem §2 |

---

## 2. SỰ THẬT VỀ "DOMAIN EVENTS TỪ CÁC MICROSERVICE KHÁC"

Tài liệu cũ liệt kê 13 event được cho là LogService đang lắng nghe:

| Service (theo tài liệu cũ) | Event được cho là có | Trạng thái thật trong repo Log |
|----------------------------|----------------------|--------------------------------|
| `UserService` | `UserRegisteredEvent`, `UserRolesChangedEvent` | ❌ Không có consumer nào; không tồn tại type trong `src/Services/Log` |
| `CatalogService` | `ProductCreatedEvent`, `StockReservedEvent`, `StockReleasedEvent` | ❌ Không có |
| `CommerceService` | `OrderSubmittedEvent`, `OrderStatusChangedEvent` | ❌ Không có |
| `AuctionService` | `BidPlacedEvent`, `AuctionEndedEvent` | ❌ Không có |
| `FulfillmentService` | `ShipmentCreatedEvent`, `ShipmentStatusUpdatedEvent` | ❌ Không có |
| `WalletService` | `PaymentCompletedEvent`, `PaymentFailedEvent` | ❌ Không có |

**Sự thật:** repo này có **đúng 1** consumer duy nhất (`GlobalLogConsumer`) với **đúng 1** message type (`ILogEventOccurred`). `grep IConsumer` trong toàn repo chỉ trả về 1 kết quả. Mọi event riêng lẻ của service khác **không** được LogService subscribe.

⇒ Để LogService thực sự nhận được log, **một trong hai** hướng phải được chốt:
- **Hướng A (khuyến nghị — hợp đồng typed):** định nghĩa các integration event dùng chung (record/DTO có `EventType`, `ServiceName`, …) và để mỗi service publish; LogService có 1 consumer generic nhận `IIntegrationLogEvent`.
- **Hướng B (giữ nguyên interface):** bắt buộc mọi service tham chiếu `Log.Domain` để publish `ILogEventOccurred` — phá vỡ tính self-contained của repo, **không khuyến nghị**.

---

## 3. BẢO ĐẢM TÍNH TOÀN VẸN VÀ CHỐNG TRÙNG LẶP (OUTBOX / INBOX)

MassTransit EF Core Outbox được đăng ký tại `Log.Infrastructure/InfrastructureDependencyInjection.cs:36-40`:

```csharp
x.AddEntityFrameworkOutbox<LogDbContext>(o =>
{
    o.UsePostgres();
    o.UseBusOutbox();
});
```

| Thành phần | Trạng thái | Bằng chứng | Ghi chú |
|------------|------------|------------|---------|
| EF Outbox đăng ký | ✅ CÓ | `InfrastructureDependencyInjection.cs:36-40` | `UsePostgres()` + `UseBusOutbox()` |
| Bảng `InboxState` (+ index `IX_InboxState_Delivered`) | ✅ CÓ | Migration `Log.Infrastructure/Migrations/20261024103313_InitialLogSchema.cs` | Dùng để chống xử lý trùng message khi RabbitMQ redeliver |
| Bảng `OutboxMessage` (+ `IX_OutboxMessage_EnqueueTime`, `IX_OutboxMessage_ExpirationTime`, `IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber`, `IX_OutboxMessage_OutboxId_SequenceNumber`) | ✅ CÓ (schema) | Cùng migration | ⚠️ Bảng tồn tại nhưng **chưa có publisher nào** ⇒ chưa được ghi thực tế |
| Bảng `OutboxState` (+ `IX_OutboxState_Created`) | ✅ CÓ (schema) | Cùng migration | — |
| Delivery count / retry | ✅ Mặc định MassTransit | Không cấu hình override trong repo | Không có `UseMessageRetry`, `UseCircuitBreaker` |

**Hệ quả cần nhớ:** "Inbox Pattern chống trùng" **chỉ có hiệu lực khi có message thật đi qua** consumer. Hiện tại 0 publisher ⇒ chưa thể nghiệm thu cơ chế này bằng test tích hợp.

---

## 4. CẤU HÌNH RABBITMQ THỰC TẾ

| Tham số | Giá trị mặc định trong code | Nguồn cấu hình | Bằng chứng |
|---------|------------------------------|----------------|------------|
| Host | `localhost` | `RabbitMq:Host` | `InfrastructureDependencyInjection.cs:44`; `Log.Api/appsettings.json:12-17` |
| Port | `5672` | `RabbitMq:Port` | `InfrastructureDependencyInjection.cs:45` |
| User | `guest` | `RabbitMq:User` | `InfrastructureDependencyInjection.cs:46` |
| Password | `string.Empty` (nếu không cấu hình) | `RabbitMq:Password` | `InfrastructureDependencyInjection.cs:47` |
| Virtual host | `/` | hard-code | `InfrastructureDependencyInjection.cs:49` |
| Endpoint naming | Kebab-case | hard-code | `InfrastructureDependencyInjection.cs:32` |
| MassTransit version | `8.3.0` | `Log.Application/Log.Application.csproj:8-15` | ⚠️ Tài liệu cũ ghi `8.4.1` là **sai** |

---

## 5. CHECKLIST KHI BỔ SUNG EVENT MỚI

- [ ] Định nghĩa message contract **additive** (không sửa property đã phát hành), đặt ở nơi dùng chung được — không nhét vào `Log.Domain` nếu service khác cần publish.
- [ ] Thêm `IConsumer<T>` vào `Log.Application/Consumers/`.
- [ ] Đăng ký `x.AddConsumer<...>()` trong `Log.Infrastructure/InfrastructureDependencyInjection.cs` (cạnh dòng `:34`).
- [ ] Ghi log qua `IApplicationDbContext` — **không** tạo Repository.
- [ ] Tôn trọng cờ `LogEventTypes.IsEnabled` trước khi ghi (xem §1.2 bước 3).
- [ ] Thêm hàng tương ứng vào bảng `§1.3` và `§2` của tài liệu này.
- [ ] Viết test tích hợp dùng `MassTransit.Testing` (⚠️ hiện **chưa có** test project — xem `specs/05`).
