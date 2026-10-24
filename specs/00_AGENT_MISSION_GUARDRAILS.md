# 00 — NHIỆM VỤ CỐT LÕI & RÀO CHẮN CHỐNG ẢO GIÁC (LOG SERVICE)

---

## 1. NHIỆM VỤ & RANH GIỚI MIỀN (BOUNDED CONTEXT)

**LogService** chịu trách nhiệm độc quyền về việc thu thập, lưu vết và truy vấn toàn bộ nhật ký hệ thống (centralized logs) và sự kiện nghiệp vụ trong hệ sinh thái Nexus:
- Ghi nhận nhật ký bất biến (`LogEntry`): Lưu vết ai (`ActorUserId`), đã làm gì (`EventType`, `Action`), trên thực thể nào (`EntityName`, `EntityId`), ở service nào (`ServiceName`), vào lúc nào (`OccurredAt`), mức độ log (`LogLevel`), và dữ liệu chi tiết JSON (`Payload`).
- Tra cứu và lọc nâng cao nhật ký (`QueryLogs`): Theo khoảng thời gian (`From`, `To`), tên service, loại sự kiện, người thực hiện, mức độ log (`LogLevel`) hoặc correlationId.
- Quản lý danh mục loại sự kiện log (`LogEventTypes`): Bật hoặc tắt tính năng ghi log của từng loại sự kiện cụ thể (`SetLogEventTypeEnabled`). Nếu một event type bị tắt (`IsEnabled = false`) $\rightarrow$ Bỏ qua không lưu log để tối ưu hiệu năng và dung lượng.
- Cung cấp dịch vụ gRPC Server tại cổng `8004` cho ApiGateway (`LogApi`).

---

## 2. NHỮNG ĐIỀU CẤM KỴ ĐỐI VỚI AI AGENTS (ANTI-HALLUCINATION GUARDRAILS)

1. **TUYỆT ĐỐI KHÔNG SỬA ĐỔI HOẶC XÓA BẢN GHI LOG (APPEND-ONLY)**:
   - Nhật ký mang tính pháp lý, bảo mật và chẩn đoán sự cố. Bảng `LogEntry` chỉ được phép thêm mới (`INSERT`) và đọc (`SELECT`). Tuyệt đối **KHÔNG CÓ API SỬA HOẶC XÓA LOG**.

2. **TUYỆT ĐỐI KHÔNG DÙNG REPOSITORY PATTERN BỌC NGOÀI EF CORE**:
   - Tuân thủ nguyên tắc kiến trúc hệ thống: Không sử dụng Repository Pattern bọc ngoài EF Core.
   - Sử dụng trực tiếp `LogDbContext` và `DbSet<T>` (hoặc interface `IApplicationDbContext`) trong tầng Application.
   - Không được tạo `ILogRepository`, `UnitOfWork`.

3. **CÔ LẬP TUYỆT ĐỐI DỮ LIỆU & RANH GIỚI MIỀN (DATABASE-PER-SERVICE)**:
   - `LogService` chỉ sở hữu cơ sở dữ liệu `log_db` trên port `5436`.
   - Mọi dữ liệu từ service khác đưa vào log thông qua RabbitMQ message payload (`Payload` dạng jsonb) hoặc gRPC call.

4. **TUYỆT ĐỐI KHÔNG THAY ĐỔI CỔNG 8004 VÀ DATABASE 5436**:
   - Service gRPC chạy tại `http://localhost:8004`.
   - Chuỗi kết nối DB trỏ tới `localhost:5436/log_db` với mật khẩu mặc định `MatkhaucuaTui123@`.

5. **TUYỆT ĐỐI KHÔNG TỰ Ý SỬA PROTOBUF MÀ KHÔNG ĐỒNG BỘ GATEWAY**:
   - RPC methods và message formats phải khớp 100% với `Contracts/shop.proto` tại ApiGateway (`LogApi`).
