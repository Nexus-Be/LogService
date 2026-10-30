# 01 — PHÂN TẦNG CLEAN ARCHITECTURE (LOG SERVICE)

> **Trạng thái triển khai (2026-10-30):** ✅ ĐÃ CHUẨN HÓA — 4 tầng Clean Architecture chuẩn hóa: Feature Co-location (`Services/Log/`), gỡ bỏ hoàn toàn MediatR, gRPC và MassTransit. Giao tiếp đồng bộ 100% RESTful qua `LogController.cs`.
> **Nguồn sự thật:** code (`Log.sln`, `Log.Api/Controllers/LogController.cs`, `Log.Application/Services/Log/LogService.cs`).
> **Cập nhật lần cuối:** 2026-10-30.

---

## 1. PHÂN TẦNG VÀ CHIỀU PHỤ THUỘC (DEPENDENCY RULE)

```
[ Log.Api ] ─────────────┐
                         │
                         ▼
             [ Log.Application ] ──▶ [ Log.Domain ]
                         ▲
                         │
[ Log.Infrastructure ] ──┘
```

| Tầng | Vai trò | Trạng thái | Bằng chứng |
|:---|:---|:---:|:---|
| `Log.Domain` | Lớp trung tâm: entity (`LogEntry`, `LogEventType`...), enum, domain exception | ✅ (zero-dependency) | Tham chiếu project: 0; package: 0 |
| `Log.Application` | Phụ thuộc `Domain`; khai báo `IApplicationDbContext`, Feature Service `LogService`, DTOs | ✅ | `Log.Application/Services/Log/LogService.cs` |
| `Log.Infrastructure` | Phụ thuộc `Application` + `Domain`; `LogDbContext`, configurations, migrations | ✅ | `Log.Infrastructure/Persistence/LogDbContext.cs` |
| `Log.Api` | Host REST API; map `LogController`, OpenTelemetry, Swagger | ✅ | `Log.Api/Controllers/LogController.cs`, `Log.Api/Program.cs` |

---

## 2. QUY CHUẨN CODE TỪNG TẦNG

| Tầng | Quy chuẩn | Trạng thái | Bằng chứng |
|:---|:---|:---:|:---|
| Domain | Bảng `Logs` là append-only, cấm cập nhật hoặc xóa log | ✅ | `Log.Domain/Entities/LogEntry.cs` |
| Application | Feature Co-location: `Services/Log/` chứa `ILogService`, `LogService`, `LogRequests`, `LogResponses` | ✅ | `Log.Application/Services/Log/**` |
| Application | Không sử dụng MediatR hoặc Generic Repository bọc ngoài EF Core | ✅ | Inject trực tiếp `IApplicationDbContext` |
| Api | 1-to-1 Controller Pairing: `LogController` chỉ inject duy nhất `ILogService` | ✅ | `Log.Api/Controllers/LogController.cs` |
| Api | Không khai báo route alias trùng lặp: chỉ dùng duy nhất `[HttpGet("logs")]` | ✅ | `Log.Api/Controllers/LogController.cs:22` |

---

## 3. QUY TRÌNH DEPENDENCY INJECTION
* `Log.Application/ApplicationDependencyInjection.cs`: Đăng ký `services.AddScoped<ILogService, LogService>()`.
* `Log.Infrastructure/InfrastructureDependencyInjection.cs`: Đăng ký `LogDbContext`, cấu hình PostgreSQL.
