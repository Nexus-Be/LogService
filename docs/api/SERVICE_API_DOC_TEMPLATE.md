# LOG SERVICE - [HTTP_METHOD] [ROUTE]

> **Feature / Use Case:** {FeatureName}
> **Trạng thái:** ⏳ DRAFT (Pre-Code Plan) | 🛠 IN_PROGRESS | ✅ VERIFIED
> **Ngày tạo:** YYYY-MM-DD
> **Tác giả:** {Author / AI Agent}

---

## 1. Thông tin Định tuyến & RBAC (Routing & Authorization)
- **HTTP Method:** `GET` | `POST`
- **Route Endpoint:** `/api/v1/logs` | `/api/v1/logs/batch` | `/api/v1/logs/event-types`
- **Quyền hạn (RBAC Policy):** `StaffOrAdmin` | `Admin` | `AllowAnonymous` (cho internal log batching)
- **Quy tắc Sở hữu (Resource Ownership):** Nhật ký hệ thống chỉ dành cho Admin và Staff quản trị.
- **Controller:** `Log.Api/Controllers/Public/LogController.cs`
- **Application Contract:** `Log.Application/Services/Log/ILogService.cs`

---

## 2. Đầu vào & Quy chuẩn Validation (Input & Validation Schema)
### 2.1 Route Parameters & Query Parameters
| Tham số | Vị trí | Kiểu C# | Bắt buộc | Ràng buộc / Giới hạn | Ghi chú |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `serviceName` | Query | `string` | Không | Max length: 50 | Lọc theo tên dịch vụ phát sinh log |
| `level` | Query | `string` | Không | PascalCase LogLevel | Lọc theo mức độ nghiêm trọng |
| `page` | Query | `int` | Không | Min: 1, Default: 1 | Tham số phân trang chuẩn |
| `pageSize` | Query | `int` | Không | Range: 1 - 100, Default: 20 | Giới hạn tối đa 100 bản ghi |

### 2.2 Request Body Record DTO
- **Vị trí file:** `Log.Application/Services/Log/LogRequests.cs`
- **Tên C# Record:** `{ActionName}Request`
- **Khai báo C# & DataAnnotations:**
```csharp
public sealed record CreateLogRequest(
    [Required(ErrorMessage = "Service name is required.")]
    string ServiceName,

    [Required(ErrorMessage = "Message is required.")]
    string Message,

    [Required(ErrorMessage = "Level is required.")]
    string Level
);
```
- **JSON Request Body mẫu (camelCase):**
```json
{
  "serviceName": "user-service",
  "message": "User registered successfully.",
  "level": "Information"
}
```

---

## 3. Đầu ra & Hợp đồng DTO thuần (Response Contract - Pure DTO)
> **LƯU Ý:** LogService CHỈ trả về DTO thuần hoặc `PagedData<T>`, KHÔNG bọc qua `ApiResponse<T>`. Vỏ `ApiResponse<T>` sẽ do API Gateway tự động đóng gói tại biên.

- **Vị trí file:** `Log.Application/Services/Log/LogResponses.cs`
- **Tên C# Record:** `{ActionName}Response` (hoặc `PagedData<{Item}Dto>`)
- **Mã HTTP thành công:** `200 OK` | `201 Created`
- **Khai báo C# Record:**
```csharp
public sealed record LogEntryResponse(
    Guid Id,
    string ServiceName,
    string Message,
    string Level, // PascalCase enum string
    DateTimeOffset Timestamp
);
```
- **JSON Response Body mẫu (Pure DTO - camelCase):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "serviceName": "user-service",
  "message": "User registered successfully.",
  "level": "Information",
  "timestamp": "2026-11-01T14:30:00Z"
}
```

---

## 4. Ma trận Mã lỗi RFC 9110 (Error Matrix)
> **Quy chuẩn:** 100% tiếng Anh, Sentence case cho `message`, SCREAMING_SNAKE_CASE cho `errorCode` (không dùng tiền tố `ERR_`).

| HTTP Status | Error Code | Message (Sentence case) | Điều kiện kích hoạt (Trigger Condition) |
| :---: | :--- | :--- | :--- |
| `400 Bad Request` | `VAL_INVALID_INPUT` | `One or more validation errors occurred.` | Vi phạm DataAnnotations trên request model |
| `401 Unauthorized` | `UNAUTHORIZED` | `Authentication is required to access this resource.` | Thiếu hoặc token không hợp lệ |
| `403 Forbidden` | `FORBIDDEN` | `You do not have permission to access or modify this resource.` | Không có vai trò Admin hoặc Staff |
| `404 Not Found` | `LOG_NOT_FOUND` | `Log entry not found with the specified identifier.` | Không tìm thấy bản ghi nhật ký |
| `500 Internal Error` | `INTERNAL_SERVER_ERROR` | `An unexpected error occurred while processing the request.` | Ngoại lệ chưa được xử lý |

### 4.1 Bảng Ánh xạ Enum liên quan (Nếu có)
| Tên Enum C# | Giá trị DB (Integer) | Giá trị API (PascalCase) | Ý nghĩa nghiệp vụ |
| :--- | :---: | :--- | :--- |
| `LogLevel.Information` | `0` | `"Information"` | Nhật ký thông tin hoạt động |
| `LogLevel.Warning` | `1` | `"Warning"` | Nhật ký cảnh báo |
| `LogLevel.Error` | `2` | `"Error"` | Nhật ký lỗi |
| `LogLevel.Critical` | `3` | `"Critical"` | Nhật ký sự cố nghiêm trọng |

---

## 5. Kế hoạch Thực thi & Checklist Kiểm chứng (Pre-Code Plan & Checklist)
### 5.1 Các bước thực thi nghiệp vụ (Execution Flow Plan)
1. **Bước 1:** Validate tham số đầu vào tại Controller boundary.
2. **Bước 2:** Kiểm tra quyền RBAC của Admin/Staff.
3. **Bước 3:** Truy vấn entity từ DbContext qua `DbSet<LogEntry>`.
4. **Bước 4:** Áp dụng bộ lọc `serviceName`, `level`, và khoảng thời gian.
5. **Bước 5:** Thực hiện phân trang trực tiếp trên SQL qua `Skip`/`Take` và `CountAsync`.
6. **Bước 6:** Trả về `PagedData<LogEntryResponse>` dạng Pure DTO.

### 5.2 Checklist hoàn thành
- [ ] **Pre-Code:** Đã đối soát code có sẵn trong LogService để tái sử dụng DTO/Enum/Naming.
- [ ] **Pre-Code:** Tài liệu API nội bộ này đã được viết và lưu tại `docs/api/{Feature}/{METHOD}_{ENDPOINT}.md`.
- [ ] **Persistence:** Tuân thủ bất biến Append-Only (không viết API sửa/xóa log).
- [ ] **Application:** Interface, Service, Request/Response DTOs đã viết xong trong `Services/Log/`.
- [ ] **Api:** Thin Controller đã ghép cặp 1-to-1, trả DTO thuần, XML summary 1 dòng.
- [ ] **Tests:** Đã viết unit/integration tests bao phủ happy path và các mã lỗi trong Error Matrix.
- [ ] **Verify:** `dotnet build Log.sln -c Release` vượt qua 100% với 0 lỗi.
