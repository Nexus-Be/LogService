<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **LogService** (414 symbols, 557 relationships, 2 execution flows).

> Index stale? Run `node .gitnexus/run.cjs analyze --index-only` from the project root.

## Always Do

- **MUST run impact before editing.** Use `impact({target: "symbolName", direction: "upstream"})` or `node .gitnexus/run.cjs impact "symbolName" --direction upstream --repo .`.
- **MUST analyze graph changes before committing.** Use `detect_changes({scope: "all"})` or `node .gitnexus/run.cjs detect-changes --scope all --repo .`.
- **MUST warn on HIGH/CRITICAL `risk` pre-edit.**
- **MUST treat `risk: UNKNOWN` as unresolved, not low.**
- **NEVER** edit before impact analysis · **NEVER** rename with find-and-replace · **NEVER** commit before detect_changes.

<!-- gitnexus:end -->

---

# QUY CHUẨN VẬN HÀNH DÀNH CHO AI CODING AGENTS (LOG SERVICE)

> **Trạng thái triển khai (2026-10-30):** ✅ ĐÃ CHUẨN HÓA — 100% RESTful API (`LogController.cs`), Feature Co-location, cấm gRPC, bãi bỏ hoàn toàn RabbitMQ, MassTransit và Outbox/Inbox tables.
> **Nguồn sự thật:** code (`Log.sln`, `Log.Api/Controllers/LogController.cs`).
> **Cập nhật lần cuối:** 2026-10-30.

---

## 🛑 NGUYÊN TẮC 1: BẮT BUỘC LẬP KẾ HOẠCH TRƯỚC KHI HÀNH ĐỘNG (PLAN-FIRST RULE)

Mọi AI Agent khi nhận nhiệm vụ kỹ thuật tại LogService **BẮT BUỘC PHẢI LẬP VÀ TRÌNH BÀY PLAN** trước khi thực hiện bất kỳ hành động sửa đổi mã nguồn, thêm/xóa file, refactor, tạo migration database hay thay đổi cấu hình nào.

Một Plan chuẩn bắt buộc phải có:
1. **Mục tiêu (Objective)**: Tóm tắt chính xác điều cần làm.
2. **Context & Files cần đọc**: Liệt kê các file nội bộ của LogService theo đúng ma trận đọc file tại Nguyên tắc 3.
3. **Phân tích rủi ro & Phạm vi ảnh hưởng (Impact Analysis)**: Dùng GitNexus `impact`.
4. **Checklist các bước thực hiện (Step-by-Step Implementation)**.
5. **Kế hoạch kiểm chứng (Verification)**: `dotnet build Log.sln -c Release`.

---

## 🧭 NGUYÊN TẮC 2: QUY TẮC PHẠM VI NỘI BỘ (INWARD-ONLY / SELF-CONTAINED)

LogService là một Git Repository độc lập:
* **TUYỆT ĐỐI CHỈ ĐƯỢC trỏ tới các tài nguyên nội bộ của LogService**.
* **TUYỆT ĐỐI KHÔNG ĐƯỢC trỏ ngược ra ngoài Root** (`../../../docs/...`) hay sang các Service khác (`../../Catalog/...`).
* Toàn bộ tài liệu, hướng dẫn và cấu hình trong repo này phải 100% tự cung tự cấp (Self-contained).

---

## 📚 NGUYÊN TẮC 3: DANH MỤC FILE NỘI BỘ BẮT BUỘC PHẢI ĐỌC TẠI LOG SERVICE

Khi bắt đầu làm việc tại LogService, AI Agent **BẮT BUỘC PHẢI ĐỌC** các file nội bộ sau theo thứ tự:
1. `specs/00_AGENT_MISSION_GUARDRAILS.md`: Bounded context nhật ký hệ thống, port `8004`, DB `5436` (`log_db`).
2. `specs/01_CLEAN_ARCHITECTURE_LAYERS.md`: Cấu trúc 4 tầng Clean Architecture, Feature Co-location, No-Repository over EF Core.
3. `specs/02_API_REST_CONTRACTS.md`: Hợp đồng RESTful API: `LogController.cs` (`/api/v1/logs`).
4. `specs/03_EVENT_MESSAGING.md`: Trạng thái bãi bỏ RabbitMQ / MassTransit; thu thập log tập trung qua OTel và REST.
5. `specs/04_DATA_PERSISTENCE.md`: Schema PostgreSQL (`Logs`, `LogEventTypes`, `CentralAuditLogs`...).
6. `specs/05_DEV_VERIFICATION_CHECKLIST.md`: Lệnh build kiểm chứng service.

---

## 🔒 NGUYÊN TẮC 4: RÀO CHẮN BẤT BIẾN TẠI LOG SERVICE

1. **TUYỆT ĐỐI KHÔNG SỬA ĐỔI HOẶC XÓA BẢN GHI LOG (APPEND-ONLY)**: Bảng `Logs` (entity `LogEntry`) chỉ được phép `INSERT` và `SELECT`. Cấm viết API sửa hoặc xóa log.
2. **TUYỆT ĐỐI KHÔNG DÙNG REPOSITORY PATTERN BỌC NGOÀI EF CORE**: Sử dụng trực tiếp `LogDbContext` và `DbSet<T>` (hoặc interface `IApplicationDbContext`) trong tầng Application. Cấm tạo `ILogRepository`, `UnitOfWork`.
3. **CẤU HÌNH PORT 8004 VÀ DATABASE 5436 LÀ BẤT BIẾN**.
4. **CẤM SỬ DỤNG gRPC VÀ BÃI BỎ MESSAGE BROKER — 100% RESTFUL API**:
   - Toàn bộ giao tiếp đồng bộ qua RESTful Web API (`LogController.cs`) với quy chuẩn 100% số nhiều cho collections: `/api/v1/logs`. Cấm gRPC, Protobuf, RabbitMQ, MassTransit.
5. **CẤM TRÙNG LẶP ROUTE ALIAS**:
   - Cấm khai báo route phụ `admin/logs` trên action `QueryLogs`. Chỉ dùng một route duy nhất `[HttpGet]` trên controller `[Route("api/v1/logs")]`.
6. **FEATURE CO-LOCATION & 1-TO-1 CONTROLLER PAIRING**:
   - Tầng Application: Toàn bộ use cases đặt tại `Log.Application/Services/Log/` gồm `ILogService.cs`, `LogService.cs`, `LogRequests.cs`, `LogResponses.cs`, `LogDtos.cs`. Cấm MediatR CQRS.
   - Tầng Api: Ghép cặp 1-to-1 với `LogController.cs`, chỉ inject duy nhất `ILogService`.
7. **QUY CHUẨN PHÂN TRANG (PAGINATION GOVERNANCE)**:
   - Toàn bộ thao tác truy vấn phân trang logs bắt buộc thực hiện trực tiếp tại Database qua `.Skip((page - 1) * pageSize).Take(pageSize)` và `.CountAsync()`.
   - Đóng gói dữ liệu trả về theo cấu trúc phân trang chuẩn `PagedData<T>` (`items` và metadata `pagination: { pageIndex, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage }`).
   - TUYỆT ĐỐI CẤM trả về danh sách log thô lớn không giới hạn để Gateway tự phân trang.
8. **QUY CHUẨN PHÂN TÁCH PUBLIC VÀ INTERNAL REST API (INTERNAL API SEGREGATION)**:
   - Cấu trúc thư mục Controller tại `Log.Api/Controllers/` bắt buộc chia thành `Controllers/Public/` (`/api/v1/logs`, `/api/v1/logs/event-types`) và `Controllers/Internal/` (`/api/v1/internal/logs/batch`).
   - Mọi endpoint nạp log nội bộ đặt tại `InternalLogController` (`POST /api/v1/internal/logs/batch`) bắt buộc qua `InternalServiceAuthMiddleware` kiểm tra header `X-Internal-Service-Key`.
   - TUYỆT ĐỐI CẤM mở endpoint nạp lô log nội bộ ra ngoài API Gateway.

---

## 💬 NGUYÊN TẮC 5: QUY CHUẨN THÔNG BÁO LỖI TOÀN HỆ THỐNG (MANDATORY ERROR & ENUM RULES)

1. **Ngôn ngữ (100% English)**: Toàn bộ thông báo lỗi viết bằng 100% tiếng Anh. Tuyệt đối CẤM dùng tiếng Việt trong response và runtime exception.
2. **Quy cách viết hoa thông báo lỗi (100% Sentence case)**:
   - Viết hoa DUY NHẤT chữ cái đầu tiên của câu thông báo.
   - Toàn bộ các từ tiếp theo viết thường, TRỪ các từ viết tắt kỹ thuật hoặc tên riêng: `ID`, `OTP`, `JWT`, `URL`, `UUID`, `Email`, `API`, `HTTP`.
   - TUYỆT ĐỐI CẤM viết hoa toàn bộ thông báo lỗi (ví dụ cấm: `"LOG NOT FOUND"`).
   - Ví dụ đúng: `"Log entry not found with the specified ID."`, `"Event type is disabled."`
3. **Mã lỗi (100% SCREAMING_SNAKE_CASE)**:
   - Viết hoa toàn bộ và nối bằng gạch dưới: `LOG_NOT_FOUND`, `EVENT_TYPE_DISABLED`, `VAL_INVALID_INPUT`, `UNAUTHORIZED`, `FORBIDDEN`.
   - CẤM tiền tố `ERR_`. Tiền tố `VAL_` chỉ dành riêng cho validation.
4. **Quy chuẩn Tuần tự hóa Enum đầu ra REST API (PascalCase Serialization)**:
   - 100% Enum trả về trong JSON envelope phải là chuỗi `PascalCase` (`"Information"`, `"Warning"`, `"Error"`), cấm chuỗi in hoa toàn bộ hoặc số nguyên.
5. **Cơ chế Duy trì Rule Bền vững ("Ghi vào Rule")**:
   - Khi Người dùng (Sếp) chỉ thị "ghi vào rule", AI Agent bắt buộc phải lập tức ghi nhận và đồng bộ vào file này, `.agents/AGENTS.md`, root `AGENTS.md` và `.cursor/rules/`.

---

## 🎯 NGUYÊN TẮC 6: ĐIỀU KIỆN HOÀN THÀNH NHIỆM VỤ (DEFINITION OF DONE)

Một task tại LogService chỉ hoàn tất khi:
* Đã lập Plan chi tiết trước khi thực hiện.
* Không chứa bất kỳ liên kết trỏ ngược ra ngoài LogService.
* `dotnet build Log.sln -c Release` thành công 100% (0 error).
* Trạng thái Git sạch, không commit file rác (`bin/`, `obj/`, `.vs/`, `.idea/`).
