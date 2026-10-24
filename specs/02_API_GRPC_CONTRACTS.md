# 02 — HỢP ĐỒNG GIAO TIẾP GRPC (LOG SERVICE GRPC CONTRACTS)

> **Trạng thái triển khai (2026-10-24):** ✅ CÓ — 4/4 rpc của `service LogApi` đã hiện thực và được map. ❌ **KHÔNG CÓ** rpc ghi log và **KHÔNG CÓ** `POST /api/v1/log/batch` (REST chỉ có 2 endpoint health/root).
> **Nguồn sự thật:** `Log.Api/Contracts/shop.proto` (364 dòng), `Log.Api/Services/LogGrpcService.cs` (135 dòng), `Log.Api/Program.cs` (69 dòng).
> **Cập nhật lần cuối:** 2026-10-24.

Hợp đồng gRPC của LogService nằm trong file duy nhất **`Log.Api/Contracts/shop.proto`** (⚠️ **không có** thư mục `Contracts/` ở cấp repo). File này chứa 9 service dùng chung; phần thuộc LogService là `service LogApi` tại dòng `72-77` và khối `// ---------- Log messages ----------` tại dòng `256-289`.

File được đưa vào build qua `GrpcServices="Server"` (`Log.Api/Log.Api.csproj:35`), sinh ra C# namespace `ShoppingCart.Contracts.V1` (`Log.Api/Contracts/shop.proto:2-3`).

---

## 1. DANH SÁCH CÁC RPC METHODS

```protobuf
// shop.proto:72-77
service LogApi {
  rpc QueryLogs(QueryLogsRequest) returns (LogPage);
  rpc GetLog(IdRequest) returns (LogEntry);
  rpc ListLogEventTypes(Empty) returns (LogEventTypeList);
  rpc SetLogEventTypeEnabled(SetLogEventTypeEnabledRequest) returns (LogEventType);
}
```

### 1.1. Trạng thái từng rpc

| # | RPC | Loại | Trạng thái | Bằng chứng (implementation) | Bằng chứng (contract) |
|---|-----|------|------------|------------------------------|------------------------|
| 1 | `QueryLogs(QueryLogsRequest) → LogPage` | Đọc (danh sách + lọc) | ✅ CÓ | `Log.Api/Services/LogGrpcService.cs:22` → `QueryLogsQuery` | `shop.proto:73` |
| 2 | `GetLog(IdRequest) → LogEntry` | Đọc (chi tiết) | ✅ CÓ | `Log.Api/Services/LogGrpcService.cs:67` → `GetLogQuery` | `shop.proto:74` |
| 3 | `ListLogEventTypes(Empty) → LogEventTypeList` | Đọc (danh mục) | ✅ CÓ | `Log.Api/Services/LogGrpcService.cs:99` → `ListLogEventTypesQuery` | `shop.proto:75` |
| 4 | `SetLogEventTypeEnabled(...) → LogEventType` | Ghi cấu hình | ✅ CÓ | `Log.Api/Services/LogGrpcService.cs:119` → `SetLogEventTypeEnabledCommand` | `shop.proto:76` |
| 5 | *rpc ghi log (WriteLog / IngestLog / AppendLog)* | Ghi dữ liệu log | ❌ CHƯA CÓ | Không tồn tại trong `LogGrpcService.cs` | Không có trong `shop.proto` |

Map vào pipeline: `app.MapGrpcService<LogGrpcService>()` tại `Log.Api/Program.cs:47`. Reflection bật ở `Log.Api/Program.cs:23` (`AddGrpcReflection`) và `:41` (`MapGrpcReflectionService`, **chỉ trong môi trường Development** — bọc bởi `if (app.Environment.IsDevelopment())` tại `:37`).

### 1.2. Hành vi lỗi từng rpc (đã kiểm chứng)

| RPC | Trường hợp lỗi | Mã trạng thái | Thông điệp thực tế | Ghi chú |
|-----|----------------|---------------|--------------------|---------|
| `GetLog` | `request.Id` không parse được thành `Guid` | `InvalidArgument` | `"ID nhật ký không hợp lệ"` | `LogGrpcService.cs:69-70` — ⚠️ **tiếng Việt**, vi phạm Nguyên tắc 5 |
| `GetLog` | Không tìm thấy bản ghi | `NotFound` | `"Không tìm thấy nhật ký tương ứng"` | `LogGrpcService.cs:73-74` — ⚠️ **tiếng Việt** |
| `SetLogEventTypeEnabled` | `request.Code` rỗng/toàn khoảng trắng | `InvalidArgument` | `"Code không được để trống"` | `LogGrpcService.cs:121-122` — ⚠️ **tiếng Việt** |
| `SetLogEventTypeEnabled` | Validator FluentValidation thất bại (ví dụ `Code` > giới hạn) | `Internal` (500) | — | ⚠️ `GlobalExceptionHandlerMiddleware` không có nhánh `ValidationException` ⇒ `ValidationException` bị map thành `INTERNAL_SERVER_ERROR` |
| `QueryLogs` | Không có kiểm tra — tham số sai (page âm, `from > to`) | — | — | Không validate; query trả về rỗng hoặc lỗi DB thô |
| `ListLogEventTypes` | — | — | — | Không có lỗi nghiệp vụ |

**Chuỗi lỗi tiếng Việt cần dịch (5 vị trí):**

| # | Vị trí | Chuỗi hiện tại | Đề xuất (English, sentence case) |
|---|--------|----------------|----------------------------------|
| 1 | `LogGrpcService.cs:70` | `ID nhật ký không hợp lệ` | `Log entry ID is invalid.` |
| 2 | `LogGrpcService.cs:74` | `Không tìm thấy nhật ký tương ứng` | `Log entry not found.` |
| 3 | `LogGrpcService.cs:122` | `Code không được để trống` | `Event type code is required.` |
| 4 | `Log.Application/Features/Commands/SetLogEventTypeEnabled/SetLogEventTypeEnabledCommand.cs:16` | `Mã sự kiện (Code) không được để trống.` | `Event type code is required.` |
| 5 | `Log.Application/Features/Commands/SetLogEventTypeEnabled/SetLogEventTypeEnabledCommand.cs:41` | `Tự động sinh cấu hình cho {Code}` | `Auto-generated configuration for {Code}.` |

> Chuỗi #5 được **lưu vào DB** (`Description` của `LogEventTypes`) và trả ra client qua `ListLogEventTypes` ⇒ là chuỗi hướng client, phải sửa cùng đợt.
> `Log.Application/Consumers/GlobalLogConsumer.cs:32,56` cũng có tiếng Việt nhưng chỉ ghi vào server log (không trả client) — ưu tiên thấp hơn.

---

## 2. CÁC MESSAGE CONTRACTS LIÊN QUAN

Trích nguyên văn từ `Log.Api/Contracts/shop.proto:255-289`:

```protobuf
// ---------- Log messages (Centralized Logging Service) ----------
message LogEntry {
  string id = 1;
  string occurred_at = 2;
  string recorded_at = 3;
  string service_name = 4;
  string event_type = 5;
  string action = 6;
  string entity_name = 7;
  string entity_id = 8;
  string actor_user_id = 9;
  string actor_username = 10;
  string actor_role = 11;
  string correlation_id = 12;
  string causation_id = 13;
  string message_id = 14;
  int32 schema_version = 15;
  string trace_id = 16;
  string payload = 17;
  string log_level = 18;
}
message LogPage { repeated LogEntry items = 1; int32 total = 2; }
message QueryLogsRequest {
  int32 page = 1; int32 page_size = 2;
  string from = 3; string to = 4;
  string service_name = 5; string event_type = 6;
  string entity_name = 7; string entity_id = 8;
  string actor_user_id = 9; string correlation_id = 10;
  string log_level = 11;
}
message LogEventType {
  string code = 1; string service_name = 2; string category = 3; string description = 4; bool is_enabled = 5;
}
message LogEventTypeList { repeated LogEventType items = 1; int32 total = 2; }
message SetLogEventTypeEnabledRequest { string code = 1; bool enabled = 2; }
```

Các message dùng chung (khai báo ở nhóm v1, `shop.proto:105-108` và `:152`):

```protobuf
message IdRequest { string id = 1; }
message Empty {}
```

### 2.1. Trạng thái từng message

| Message | Trạng thái | Bằng chứng | Ghi chú |
|---------|------------|------------|---------|
| `LogEntry` (18 field) | ✅ CÓ | `shop.proto:256-275`; map 1-1 trong `LogGrpcService.cs:41-61` và `:76-96` | `schema_version` là `int32`, hiện luôn gán từ entity |
| `LogPage` | ✅ CÓ | `shop.proto:276` | `total` là tổng số bản ghi khớp filter |
| `QueryLogsRequest` (11 field) | ✅ CÓ | `shop.proto:277-284` | `from`/`to` là `string` (ISO-8601), **không** phải `google.protobuf.Timestamp` |
| `LogEventType` (5 field) | ✅ CÓ | `shop.proto:285-287` | — |
| `LogEventTypeList` | ✅ CÓ | `shop.proto:288` | — |
| `SetLogEventTypeEnabledRequest` | ✅ CÓ | `shop.proto:289` | — |

---

## 3. BỀ MẶT REST THỰC TẾ (KHÔNG PHẢI GRPC)

`Log.Api/Program.cs` (69 dòng) chỉ map **2 minimal endpoint**, không có controller nào:

| # | Method | Route | Trạng thái | Bằng chứng | Trả về |
|---|--------|-------|------------|------------|--------|
| 1 | `GET` | `/health` | ✅ CÓ | `Log.Api/Program.cs:66` | `{ status: "Healthy", service: "Log.Api" }` — ⚠️ **hằng số**, không kiểm tra DB |
| 2 | `GET` | `/` | ✅ CÓ | `Log.Api/Program.cs:67` | Chuỗi hướng dẫn dùng gRPC client |
| 3 | `POST` | `/api/v1/log/batch` | ❌ **KHÔNG TỒN TẠI** | Không có `MapPost`, 0 controller | — |
| 4 | `GET` | `/swagger` (Development) | ✅ CÓ | `Log.Api/Program.cs:37-41` | Swagger UI |

### 3.1. Cảnh báo về `POST /api/v1/log/batch`

Một số tài liệu (ở cấp root repo, **không** phải trong repo Log) quảng cáo endpoint `POST /api/v1/log/batch` để ghi log hàng loạt, và phía Gateway có thể đang gọi tới đường dẫn này. **Sự thật trong code:**

- `Log.Api/Program.cs` chỉ có `MapGet` (2 lần), **không** có `MapPost`, `MapControllers` hay `AddControllers`.
- Không có file `*Controller.cs` nào trong `Log.Api/`.
- ⇒ Mọi lời gọi `POST /api/v1/log/batch` sẽ nhận **404** (hoặc bị `UseHttpsRedirection` chặn trước).

**Muốn triển khai endpoint này**, cần thêm tối thiểu:
1. `builder.Services.AddControllers()` (hoặc dùng `MapPost` minimal API) trong `Log.Api/Program.cs`.
2. Một `BatchLogsCommand` + handler trong `Log.Application/Features/Commands/`.
3. Ghi qua `IApplicationDbContext`/`LogDbContext` trực tiếp (không Repository Pattern).
4. Bổ sung validator (giới hạn số bản ghi/lô, kích thước `Payload`).
5. Cặp thông báo lỗi tiếng Anh + mã lỗi `VAL_INVALID_INPUT` / `LOG_BATCH_EMPTY`.

---

## 4. PIPELINE XỬ LÝ REQUEST (THỨ TỰ THẬT)

Thứ tự trong `Log.Api/Program.cs` quyết định hành vi runtime:

```
AddServiceObservability (OTel)          :12
AddApplicationServices                  :15
AddInfrastructureServices               :16
AddGrpc + EnableDetailedErrors(Dev)     :19-22
AddGrpcReflection                       :23
AddEndpointsApiExplorer + AddSwaggerGen :26-27
ConfigureHttpJsonOptions (enum→string)  :28-31
─────────────────── build ───────────────────
UseGlobalExceptionHandler               :35
[Dev] UseSwagger + UseSwaggerUI + MapGrpcReflectionService :37-42
UseHttpsRedirection                     :44
MapGrpcService<LogGrpcService>()        :47
Database.Migrate() trong try/catch      :50-64
MapGet("/health")                       :66
MapGet("/")                             :67
```

Hai điểm cần lưu ý khi viết tài liệu/hướng dẫn:

1. **`UseHttpsRedirection()` ở dòng 44** — khi chạy local không có HTTPS cert/port, request HTTP có thể bị redirect sang `https://localhost:7103` (`launchSettings.json:27`). Đây là lý do phổ biến khiến client gọi `http://localhost:5280` thất bại.
2. **Auto-migrate ở dòng 50-64** — nếu DB chưa sẵn sàng, exception bị nuốt và chỉ log warning `"Database migration deferred on startup: {Message}"`, service **vẫn khởi động**. `/health` vẫn trả `Healthy` dù DB chết.

---

## 5. QUY TẮC THAY ĐỔI HỢP ĐỒNG (CONTRACT CHANGE RULES)

1. **Chỉ được thay đổi ADDITIVE**: thêm field với số mới, thêm message, thêm rpc. **Cấm** đổi số field, đổi kiểu field, đổi tên field đã phát hành (`shop.proto:5-7` ghi rõ: `SchemaVersion: v3 — mọi thay đổi từ đây phải là ADDITIVE`).
2. **Cấm tự ý sửa `shop.proto` khi chưa chốt nguồn sự thật duy nhất** — hệ thống hiện có nhiều bản `shop.proto` khác hash; xem cảnh báo ở `specs/00` §4.
3. Mọi rpc mới phải: thêm vào `service LogApi`, thêm message request/response, override trong `LogGrpcService`, và bổ sung dòng vào bảng `§1.1` của tài liệu này.
4. Thông điệp lỗi mới **phải** bằng tiếng Anh, sentence case, kèm mã lỗi SCREAMING_SNAKE_CASE (Nguyên tắc 5).
