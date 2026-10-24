# LOG SERVICE — ARCHITECTURE & FOLDER STRUCTURE GUIDE
## (CẨM NANG CẤU TRÚC THƯ MỤC & PHÁT TRIỂN DÀNH CHO DEVELOPER VÀ AI AGENT)

> **Thông tin dịch vụ**:
> - **Service Port**: `8004` (HTTP/1.1 REST & HTTP/2 gRPC)
> - **Database**: PostgreSQL (`log_db`), Port máy host: `5436`
> - **Nhiệm vụ chính**: Lưu vết nhật ký tập trung (Centralized log trail), Cảnh báo bảo mật (SecurityAuditAlert), Thu thập nhật ký hàng loạt (Batch Ingestion).

---

## 1. SƠ ĐỒ PHÂN TẦNG VÀ CẤU TRÚC THƯ MỤC CHUẨN

```
src/Services/Log/
│
├── Log.Domain/                                        # 1. TẦNG DOMAIN (POCO 100%, ZERO DEPENDENCY)
│   ├── Base/                                          # BaseEntity, IAuditable
│   ├── Entities/                                      # LogEntry, CentralAuditLog, SecurityAuditAlert...
│   ├── Enums/                                         # LogSeverity, AuditAction, AlertStatus...
│   └── Exceptions/                                    # DomainException, BusinessRuleValidationException
│
├── Log.Application/                                   # 2. TẦNG APPLICATION (USE CASES & BUSINESS LOGIC)
│   ├── Common/                                        # Behaviors, Interfaces (IApplicationDbContext)
│   ├── Consumers/                                     # MassTransit Event Consumers
│   ├── DTOs/                                          # Data Transfer Objects
│   ├── Features/                                      # Commands & Queries
│   └── ApplicationDependencyInjection.cs              # Đăng ký AddApplicationServices()
│
├── Log.Infrastructure/                                # 3. TẦNG INFRASTRUCTURE (CÔNG NGHỆ & HẠ TẦNG CỤ THỂ)
│   ├── Persistence/                                   # Quản lý CSDL PostgreSQL & EF Core 8
│   │   ├── LogDbContext.cs                            # Triển khai IApplicationDbContext
│   │   ├── Configurations/                            # 6 Fluent API Configurations (*Configuration.cs)
│   │   └── Seeds/                                     # Dữ liệu hạt giống loại nhật ký
│   └── InfrastructureDependencyInjection.cs           # Đăng ký AddInfrastructureServices(configuration)
│
└── Log.Api/                                           # 4. TẦNG API / HOST (ENTRY POINT & MIDDLEWARE)
    ├── Middleware/                                    # MIDDLEWARE TOÀN CỤC CỦA SERVICE
    │   └── GlobalExceptionHandlerMiddleware.cs        # Bắt mọi lỗi ngoại lệ, format RFC 9110 ProblemDetails
    ├── Services/                                      # gRPC Server Service (LogGrpcService.cs)
    ├── Contracts/                                     # shop.proto (Hợp đồng gRPC)
    └── Program.cs                                     # Khởi động Kestrel HTTP/1 & HTTP/2, gọi DI App & Infra
```

---

## 2. NGUYÊN TẮC BẤT BIẾN TẠI LOG SERVICE

1. **APPEND-ONLY (CHỈ THÊM, KHÔNG SỬA/XÓA)**: Nhật ký hệ thống chỉ cho phép `INSERT` và `SELECT`. Tuyệt đối cấm viết API sửa đổi hoặc xóa bản ghi log.
2. **BATCHING INGESTION**: Thu gom log theo lô (Batch) thay vì ghi từng dòng đơn lẻ để tránh cạn kiệt kết nối mạng và I/O đĩa.
3. **GLOBAL MIDDLEWARE**: `GlobalExceptionHandlerMiddleware` bắt mọi ngoại lệ và chuẩn hóa ProblemDetails.

---

## 3. CÁCH CHẠY VÀ KIỂM THỬ ĐỘC LẬP SERVICE

```bash
# 1. Bật database của Log Service (Port 5436):
cd src/Services/Log
docker compose up -d log-db

# 2. Tạo & Cập nhật migration:
./.tools/dotnet-ef migrations add InitialLogSchema --project Log.Infrastructure --startup-project Log.Api
./.tools/dotnet-ef database update --project Log.Infrastructure --startup-project Log.Api

# 3. Chạy service:
dotnet run --project Log.Api
```
