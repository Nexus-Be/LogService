# 05 — DANH SÁCH KIỂM CHỨNG & NGHIỆM THU (LOG SERVICE CHECKLIST)

> **Trạng thái triển khai (2026-10-24):** ⚠️ Một phần — `dotnet build Log.sln -c Release` là bước kiểm chứng **khả dụng duy nhất**; ❌ **repo có 0 test project** nên `dotnet test` không có gì để chạy; ❌ CI (`.github/workflows/ci-cd.yml`) chỉ build/push Docker, **không** chạy build .NET cũng không chạy test.
> **Nguồn sự thật:** `Log.sln` (4 project), `Dockerfile` (23 dòng), `docker-compose.yml` (59 dòng), `.github/workflows/ci-cd.yml` (47 dòng), `Log.Api/Program.cs` (69 dòng), `Log.Api/Properties/launchSettings.json` (41 dòng).
> **Cập nhật lần cuối:** 2026-10-24.

---

## 1. LỆNH BIÊN DỊCH & CHẠY THỬ NGHIỆM

```bash
# 0. Yêu cầu: .NET SDK 8.0 (mọi project đều TargetFramework=net8.0)

# 1. Biên dịch TOÀN BỘ solution (4 project, 0 test project)
dotnet build Log.sln -c Release

# 2. Dựng DB PostgreSQL của riêng service (port 5436, database log_db)
docker compose up -d log-db
#    hoặc tự cấu hình DB ngoài rồi sửa ConnectionStrings:DefaultConnection trong Log.Api/appsettings.json

# 3. Chạy service ở ĐÚNG port hợp đồng 8004 (bỏ qua launchSettings)
ASPNETCORE_URLS=http://localhost:8004 dotnet run --project Log.Api --no-launch-profile
```

⚠️ **Bẫy port:** nếu chạy `dotnet run --project Log.Api` mà **không** có `--no-launch-profile`, ASP.NET Core đọc `Log.Api/Properties/launchSettings.json`:

| Profile | URL thật | Bằng chứng |
|---------|----------|------------|
| `http` | `http://localhost:5280` | `launchSettings.json:17` |
| `https` | `https://localhost:7103` + `http://localhost:5280` | `launchSettings.json:27` |
| `IIS Express` | `http://localhost:61597`, ssl `44337` | `launchSettings.json:7-8` |
| **Docker** | **`http://localhost:8004`** | `Dockerfile:3-4` (`EXPOSE 8004`, `ASPNETCORE_URLS=http://+:8004`); `docker-compose.yml:37` |

⇒ Chạy local **không** tự lên `8004` như tài liệu cũ mô tả. Phải dùng `ASPNETCORE_URLS` hoặc `--no-launch-profile` để khớp cấu hình Gateway.

⚠️ **Bẫy HTTPS redirect:** pipeline có `app.UseHttpsRedirection()` (`Log.Api/Program.cs:44`). Với profile `https`, request HTTP sẽ bị `307` sang `https://localhost:7103`; với profile `http` (không cấu hình cổng HTTPS) middleware chỉ log cảnh báo `Failed to determine the https port for redirect` và cho request đi qua.

---

## 2. KIỂM CHỨNG GIAO DIỆN (SMOKE TEST)

### 2.1. Endpoint REST

```bash
# Phải trả {"status":"Healthy","service":"Log.Api"}
curl -s http://localhost:8004/health

# Phải trả chuỗi hướng dẫn dùng gRPC client
curl -s http://localhost:8004/

# PHẢI TRẢ 404 — endpoint này KHÔNG TỒN TẠI trong code
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:8004/api/v1/log/batch \
  -H "Content-Type: application/json" -d '[]'
```

| # | Kiểm chứng | Kỳ vọng | Trạng thái tính năng |
|---|------------|---------|----------------------|
| 1 | `GET /health` | `200` + `{"status":"Healthy","service":"Log.Api"}` | ✅ CÓ (`Program.cs:66`) — ⚠️ **hằng số**, không kiểm tra DB |
| 2 | `GET /` | `200` + chuỗi hướng dẫn gRPC | ✅ CÓ (`Program.cs:67`) |
| 3 | `POST /api/v1/log/batch` | **404** | ❌ KHÔNG TỒN TẠI (xem `specs/02` §3.1) |
| 4 | `GET /swagger` (Development) | `200` Swagger UI | ✅ CÓ (`Program.cs:37-40`) |

### 2.2. gRPC (cần `grpcurl`)

Reflection chỉ bật trong Development (`Log.Api/Program.cs:37-41`, `if (app.Environment.IsDevelopment())`). Khi chạy local mặc định `ASPNETCORE_ENVIRONMENT=Development` (theo launchSettings), reflection khả dụng.

```bash
# Liệt kê service — kỳ vọng thấy shoppingcart.v1.LogApi
grpcurl -plaintext localhost:8004 list

# Liệt kê method — kỳ vọng đúng 4 method
grpcurl -plaintext localhost:8004 list shoppingcart.v1.LogApi

# 1) QueryLogs
grpcurl -plaintext -d '{"page":1,"page_size":20}' \
  localhost:8004 shoppingcart.v1.LogApi/QueryLogs

# 2) GetLog — dùng một Id thật lấy từ QueryLogs
grpcurl -plaintext -d '{"id":"<GUID>"}' \
  localhost:8004 shoppingcart.v1.LogApi/GetLog

# 3) ListLogEventTypes — kỳ vọng 5 bản ghi seed
grpcurl -plaintext -d '{}' \
  localhost:8004 shoppingcart.v1.LogApi/ListLogEventTypes

# 4) SetLogEventTypeEnabled — tắt rồi bật lại USER_REGISTER
grpcurl -plaintext -d '{"code":"USER_REGISTER","enabled":false}' \
  localhost:8004 shoppingcart.v1.LogApi/SetLogEventTypeEnabled
```

| # | rpc | Kỳ vọng | Trạng thái |
|---|-----|---------|------------|
| 1 | `QueryLogs` | `200` + `LogPage` (rỗng nếu chưa có log) | ✅ CÓ |
| 2 | `GetLog` với Id không phải Guid | `InvalidArgument` + **`ID nhật ký không hợp lệ`** (tiếng Việt ⚠️) | ✅ CÓ |
| 3 | `GetLog` với Guid không tồn tại | `NotFound` + **`Không tìm thấy nhật ký tương ứng`** (tiếng Việt ⚠️) | ✅ CÓ |
| 4 | `ListLogEventTypes` | `5` item seed, `Description` tiếng Việt | ✅ CÓ |
| 5 | `SetLogEventTypeEnabled` với `code` rỗng | `InvalidArgument` + **`Code không được để trống`** (tiếng Việt ⚠️) | ✅ CÓ |
| 6 | `SetLogEventTypeEnabled` với `code` dài quá giới hạn | `Internal` (500) — vì middleware không map `ValidationException` | ⚠️ HÀNH VI SAI cần sửa |

### 2.3. Kiểm chứng OpenTelemetry

- Cấu hình `AddServiceObservability(builder.Configuration, "Log.Api")` tại `Log.Api/Program.cs:12`; hiện thực tại `Log.Api/Extensions/ObservabilityExtensions.cs:8-25`.
- Endpoint OTLP mặc định `http://localhost:4317` (`Log.Api/appsettings.json:18-20`; fallback trong `ObservabilityExtensions.cs:10`).
- Docker override bằng biến `OTEL_EXPORTER_OTLP_ENDPOINT`, mặc định `http://tempo:4317` (`docker-compose.yml:47`).
- Cách nghiệm thu: chạy một collector (Tempo/Jaeger/otel-collector) ở `4317`, gọi 1 rpc, rồi tìm span tên service `Log.Api`.

---

## 3. CHECKLIST TRƯỚC KHI COMMIT CHO AI AGENTS

### 3.1. Bắt buộc (đã kiểm chứng được)

- [ ] **Biên dịch thành công**: `dotnet build Log.sln -c Release` → 0 error.
- [ ] **Không tạo file ngoài phạm vi**: chỉ sửa `.md` khi task là tài liệu; chỉ sửa `.cs` khi task là code.
- [ ] **Tính bất biến (Append-Only)**: không thêm rpc/endpoint nào `UPDATE`/`DELETE` trên `Logs`.
- [ ] **No-Repository Pattern**: không xuất hiện `ILogRepository`, `IUnitOfWork`, `IRepository<`.
- [ ] **Tên solution đúng**: `Log.sln` — **không** phải `LogService.sln`.
- [ ] **Git sạch**: không commit `bin/`, `obj/`, `.vs/`, `.idea/`, `.env`.

### 3.2. Cần sửa (hiện ĐANG VI PHẠM — ghi nhận, không tự ý sửa khi làm task tài liệu)

- [ ] ❌ **Thông báo lỗi 100% English**: còn 5 chuỗi tiếng Việt (`LogGrpcService.cs:70,74,122`; `SetLogEventTypeEnabledCommand.cs:16,41`) — xem `specs/02` §1.2.
- [ ] ❌ **Mô tả seed English**: 5 `Description` tiếng Việt trong `LogSeedData.cs` — xem `specs/04` §4.1.
- [ ] ❌ **Index hiệu năng**: `0/5` index trên `Logs` — xem `specs/04` §3.
- [ ] ❌ **Test project**: `0` — xem §4.
- [ ] ❌ **`Log.Api/Log.Api.http`**: còn `GET {{Log.Api_HostAddress}}/weatherforecast/` (`Log.Api/Log.Api.http:3`) → luôn **404**; phải đổi sang `/health` hoặc một rpc mẫu.
- [ ] ❌ **Endpoint batch**: nếu Gateway thật sự gọi `POST /api/v1/log/batch` thì phải hiện thực hoặc sửa phía gọi — xem `specs/02` §3.1.
- [ ] ❌ **Đường ghi log**: `GlobalLogConsumer` không có publisher — xem `specs/03` §1.3.
- [ ] ❌ **Đường dẫn `.tools/dotnet-ef`**: **không tồn tại** trong repo. Dùng `dotnet ef` (global tool) hoặc `dotnet ef` qua `Microsoft.EntityFrameworkCore.Tools` (`Log.Infrastructure/Log.Infrastructure.csproj:16`).
- [ ] ❌ **`ValidationException` → 500**: `Log.Api/Middleware/GlobalExceptionHandlerMiddleware.cs:58-62` thiếu nhánh `FluentValidation.ValidationException` ⇒ phải trả `InvalidArgument`/`400` + `VAL_INVALID_INPUT`.
- [ ] ⚠️ **Port local lệch hợp đồng**: `5280`/`7103` (launchSettings) vs `8004` (Docker/spec).

---

## 4. VÌ SAO `dotnet test` KHÔNG CHẠY ĐƯỢC (0 TEST PROJECT)

| Sự thật | Bằng chứng |
|---------|------------|
| `Log.sln` chỉ có **4 project**, không có project test | `Log.sln` chứa đúng `Log.Api`, `Log.Application`, `Log.Domain`, `Log.Infrastructure` |
| Không có package test nào (`xunit`, `nunit`, `MSTest`, `FluentAssertions`, `Moq`, `MassTransit.Testing`) | 4 file `.csproj` không tham chiếu package test |
| CI không chạy `dotnet test` | `.github/workflows/ci-cd.yml` chỉ có `docker/build-push-action` (dòng 38-47) |
| Dockerfile không build cả solution | `Dockerfile:15` build `Log.Api/Log.Api.csproj` (không phải `Log.sln`) |

⇒ Lệnh `dotnet test Log.sln -c Release` **thoát 0 nhưng không chạy test nào** — báo "xanh" ở đây là **vô nghĩa**, không được dùng làm bằng chứng nghiệm thu.

### 4.1. Đề xuất bổ sung (khi được phép sửa code)

| Project đề xuất | Nội dung tối thiểu | Ghi chú |
|------------------|---------------------|---------|
| `Log.UnitTests` | `QueryLogsQueryHandler` (page/pageSize clamp, filter, ordering), `SetLogEventTypeEnabledCommand` validator, `GlobalLogConsumer.Consume` (skip khi `IsEnabled=false`) | `xunit` + `FluentAssertions` + EF Core InMemory hoặc SQLite in-memory |
| `Log.IntegrationTests` | 4 rpc qua `WebApplicationFactory` + `Grpc.Net.Client`; migration lên Postgres thật (Testcontainers) | Cần Docker trong CI |

Sau khi có test: cập nhật `Log.sln`, thêm bước `dotnet test` vào `.github/workflows/ci-cd.yml`, và bỏ cảnh báo "0 test" ở banner các tài liệu.

---

## 5. KIỂM CHỨNG BẰNG DOCKER (ĐƯỜNG ĐÚNG PORT 8004)

```bash
# Dựng DB + service (map 8004:8004)
docker compose up -d

# Kiểm tra
curl -s http://localhost:8004/health
docker compose logs -f log-service

# Dừng
docker compose down          # giữ volume log_db_data
docker compose down -v       # xóa luôn dữ liệu
```

| Thành phần | Giá trị | Bằng chứng |
|------------|---------|------------|
| Image DB | `postgres:16-alpine` | `docker-compose.yml:12` |
| Container DB | `nexus-log-db` | `docker-compose.yml:13` |
| Port DB | `5436:5432` (override `LOG_DB_PORT`) | `docker-compose.yml:19` |
| Container app | `nexus-log-service` | `docker-compose.yml:35` |
| Port app | `8004:8004` (override `LOG_SERVICE_PORT`) | `docker-compose.yml:37` |
| Volume | `nexus-be_log_db_data` | `docker-compose.yml:58-59` |
| Network | `nexus-network` | `docker-compose.yml:53-55` |
| Healthcheck DB | `pg_isready` — interval 1m, timeout 30s, retries 5, start_period 60s | `docker-compose.yml:23-28` |
| Phụ thuộc | `log-service` chờ `log-db` healthy | `docker-compose.yml:48-50` |
| Biến bí mật | `${POSTGRES_PASSWORD}`, `RABBITMQ_PASSWORD` (mặc định **hard-coded trong compose** ⚠️) | `docker-compose.yml:17,42,46` |

⚠️ **Hạ tầng local có thể hỏng độc lập với code:** image `postgres:16-alpine` trên một số máy dev bị lỗi kiến trúc (`exec format error`) khiến container `Restarting (255)` — khi đó `/health` của app vẫn trả `Healthy` vì không kiểm tra DB. Đừng coi `/health` là bằng chứng DB sống.

---

## 6. TRẠNG THÁI CI/CD

| Hạng mục | Trạng thái | Bằng chứng |
|----------|------------|------------|
| Workflow tồn tại | ✅ CÓ | `.github/workflows/ci-cd.yml` (47 dòng) |
| Trigger | ✅ CÓ | push/PR lên `main` (`:3-7`) |
| Docker build + push | ✅ CÓ | `docker/build-push-action@v5` (`:38-47`), multi-arch `linux/amd64,linux/arm64` (`:45`) |
| `dotnet build` trong CI | ❌ CHƯA CÓ | Không có step `setup-dotnet`, không có `dotnet build` |
| `dotnet test` trong CI | ❌ CHƯA CÓ | Không có step test (và cũng không có test project) |
| Gate chất lượng (lint/format/coverage) | ❌ CHƯA CÓ | — |

⇒ Hiện tại một PR có thể **build hỏng** vẫn qua CI, miễn là Docker build được. Đây là khoảng trống cần bổ sung đầu tiên khi được phép sửa `.yml`.

---

## 7. ĐỊNH NGHĨA "XONG" CHO TASK TÀI LIỆU TẠI REPO NÀY

- [ ] Mọi file đã sửa đều là `.md`.
- [ ] Mọi bảng rpc / endpoint / event / bảng DB đều có cột `Trạng thái` + `Bằng chứng` dạng `file:line`.
- [ ] Không còn phát biểu sai: `POST /api/v1/log/batch`, `LogEntries`, `logs`, `LogSeverity`, `AuditAction`, `LogService.sln`, `MassTransit 8.4.1`, `.tools/dotnet-ef`, "đã có 5 index", "có test".
- [ ] Banner có đủ 3 dòng: trạng thái (✅/⚠️/❌ + số liệu), nguồn sự thật, ngày cập nhật.
- [ ] `AGENTS.md` và `CLAUDE.md` **giống hệt nhau** (byte-identical).
- [ ] `git diff --stat` chỉ chứa `.md`.
