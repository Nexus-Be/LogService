# 02 — HỢP ĐỒNG GIAO TIẾP RESTFUL (LOG SERVICE REST CONTRACTS)

> **Trạng thái triển khai (2026-10-30):** ✅ ĐÃ CHUYỂN ĐỔI 100% RESTFUL — Toàn bộ giao tiếp truy vấn và ghi nhận nhật ký qua RESTful Web API (`LogController.cs`). Bãi bỏ hoàn toàn gRPC, Protobuf (`shop.proto`) và RabbitMQ / MassTransit.
> **Nguồn sự thật:** code (`Log.Api/Controllers/LogController.cs`).
> **Cập nhật lần cuối:** 2026-10-30.

---

## 1. DANH MỤC ENDPOINTS RESTFUL CHÍNH THỨC

### 1.1. Truy vấn & Quản trị Nhật ký (`LogController` — `/api/v1`)
* `GET /api/v1/logs`: Truy vấn danh sách nhật ký hệ thống có phân trang và bộ lọc linh hoạt (level, service, entity, correlationId...).
* `GET /api/v1/logs/{id}`: Xem chi tiết một bản ghi nhật ký theo mã định danh.
* `GET /api/v1/logs/event-types`: Liệt kê danh mục các loại sự kiện hệ thống đã đăng ký.
* `PUT /api/v1/logs/event-types/{code}/enabled`: Bật hoặc tắt trạng thái ghi log cho một loại sự kiện cụ thể.

---

## 2. NGUYÊN TẮC BẤT BIẾN

* **Append-Only**: Cấm cập nhật hoặc xóa dữ liệu bản ghi trong bảng `Logs`.
* **No Duplicate Route Aliases**: Endpoint truy vấn logs dùng duy nhất route chính thức `[HttpGet("logs")]`. Cấm khai báo route phụ `admin/logs`.
* **Envelope & Errors**: Phản hồi bọc trong phong bì `ApiResponse<T>` hoặc `ApiResponse<PagedData<T>>`. Lỗi sử dụng mã `SCREAMING_SNAKE_CASE` và câu tiếng Anh Sentence case.
