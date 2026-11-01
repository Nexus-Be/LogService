---
name: service-api-doc
description: Pre-code API documentation standard for LogService (port 8004). Mandates drafting a service-level technical API doc before coding, referencing existing domain entities, enums, DTOs, and error patterns for system-wide consistency. Governs pure DTO contracts, validation attributes, and RFC 9110 error matrices inside LogService.
---

# LogService API Pre-Code Documentation Skill

> **Scope:** LogService repository only (`src/Services/Log/`). 100% Inward-Only. Never reference the root workspace or sibling services.
> **Bất biến Append-Only:** Bảng `Logs` chỉ được phép `INSERT` và `SELECT`. Tuyệt đối cấm API sửa hoặc xóa nhật ký.

This skill governs the **mandatory pre-code documentation lifecycle** within LogService. Before any C# code is written or modified for a REST endpoint, the developer or AI agent must create a technical API specification inside `docs/api/`.

---

## 1. Ranh giới Tài liệu LogService

* **Vị trí lưu trữ:** `docs/api/{Feature}/{HTTP_METHOD}_{ENDPOINT_SLUG}.md` (ví dụ `docs/api/Logs/GET_LOGS.md`).
* **Thời điểm lập:** **BẮT BUỘC TRƯỚC KHI VIẾT CODE (Pre-Code)**.
* **Định dạng dữ liệu trả về:** **Pure DTO** hoặc `PagedData<T>` (tuyệt đối không bọc qua `ApiResponse<T>`).
* **Rào chắn Inward-Only:** Chỉ trỏ tới tài nguyên nội bộ của LogService (`Log.Domain`, `Log.Application`, `Log.Infrastructure`, `Log.Api`). Không tham chiếu tới `docs/api/` ở Root.

---

## 2. Quy trình 3 Bước Tra cứu Source of Truth Trước Khi Lập Doc

### Bước 1: Tra cứu Thực thể Domain (`Log.Domain/Entities/`)
- Mở `Log.Domain/Entities/` (ví dụ `LogEntry.cs`, `LogEventType.cs`, `CentralAuditLog.cs`).
- Ràng buộc: `LogLevel`, timestamp, payload jsonb, service name.

### Bước 2: Tra cứu Bảng mã Enum (`Log.Domain/Enums/`)
- Mở `Log.Domain/Enums/` (ví dụ `LogLevel.cs`).
- Ghi nhận số nguyên DB (`0`, `1`, `2`...) và chuỗi PascalCase API (`Information`, `Warning`, `Error`, `Critical`).
- Điền bảng ánh xạ vào Mục 4.1 của doc.

### Bước 3: Tra cứu Mẫu DTO & Xử lý Lỗi Có Sẵn (`Log.Application/`)
- Rà soát các tính năng có sẵn tại `Log.Application/Services/Log/`.
- Kế thừa quy ước đặt tên DTO: `LogQueryRequest`, `LogEntryResponse`, `BatchLogRequest`.
- Kế thừa mã lỗi đã có (`LOG_NOT_FOUND`, `EVENT_TYPE_DISABLED`, `VAL_INVALID_INPUT`).

---

## 3. Các Rào Chắn Bất Biến tại LogService

1. **Append-Only Invariant:** Cấm tuyệt đối viết API sửa (PUT/PATCH) hoặc xóa (DELETE) bản ghi nhật ký trong bảng `Logs`.
2. **Pure DTO:** Controller tại LogService trả về DTO thuần, cấm thêm `ResultFilter` hay bọc `ApiResponse<T>`.
3. **Plural Route & Single Alias:** Sử dụng route chính thức `/api/v1/logs`. Cấm trùng lặp alias `admin/logs`.
4. **Mã lỗi & Message:** 100% tiếng Anh, Sentence case cho `message`, SCREAMING_SNAKE_CASE cho `errorCode` (cấm tiền tố `ERR_`).
5. **Phân trang tại DB:** Toàn bộ danh sách log phân trang tại DB qua `Skip`/`Take` và `CountAsync`, trả về `PagedData<T>`.

---

## 4. Quy trình Thực hiện (Workflow)

1. Sao chép `docs/api/SERVICE_API_DOC_TEMPLATE.md` vào `docs/api/{Feature}/{HTTP_METHOD}_{ENDPOINT_SLUG}.md`.
2. Điền đầy đủ 5 phần của template dựa trên kết quả tra cứu Source of Truth.
3. Hoàn thiện mục 5.1 (Pre-Code Plan) mô tả các bước xử lý kỹ thuật.
4. Triển khai code C# tuân thủ 100% hợp đồng đã đặc tả trong doc.
5. Chạy `dotnet build Log.sln -c Release` đạt 100% pass, tích checklist và chuyển trạng thái doc sang `✅ VERIFIED`.
