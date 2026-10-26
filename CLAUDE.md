<!-- gitnexus:start -->
# GitNexus — Code Intelligence
This project is indexed by GitNexus as **LogService** (414 symbols, 557 relationships, 2 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.
> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).
## Always Do
- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows. For regression review, compare against the default branch: `detect_changes({scope: "compare", base_ref: "main"})`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).
## Never Do
- NEVER edit a function, class, or method without first running `impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit changes without running `detect_changes()` to check affected scope.
## Resources
| Resource | Use for |
|----------|---------|
| `gitnexus://repo/LogService/context` | Codebase overview, check index freshness |
| `gitnexus://repo/LogService/clusters` | All functional areas |
| `gitnexus://repo/LogService/processes` | All execution flows |
| `gitnexus://repo/LogService/process/{name}` | Step-by-step execution trace |
## CLI
| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |
<!-- gitnexus:end -->
> **Trạng thái triển khai (2026-10-24):** ⚠️ Một phần — 4/4 rpc gRPC ✅, OTel ✅, 9 bảng DB ✅; REST batch ❌ (không có `POST /api/v1/log/batch`), consumer `ILogEventOccurred` ⚠️ (0 publisher), 0/5 index ❌, 0 test project ❌.
> **Nguồn sự thật:** code trong repo này (`file:line` ghi kèm trong từng bảng).
> **Cập nhật lần cuối:** 2026-10-24.
---
# QUY CHUẨN VẬN HÀNH DÀNH CHO AI CODING AGENTS (LOG SERVICE)
---
## 🛑 NGUYÊN TẮC 1: BẮT BUỘC LẬP KẾ HOẠCH TRƯỚC KHI HÀNH ĐỘNG (PLAN-FIRST RULE)
Mọi AI Agent khi nhận nhiệm vụ kỹ thuật tại LogService **BẮT BUỘC PHẢI LẬP VÀ TRÌNH BÀY PLAN** trước khi thực hiện bất kỳ hành động sửa đổi mã nguồn, thêm/xóa file, refactor, tạo migration database hay thay đổi cấu hình nào.
Một Plan chuẩn bắt buộc phải có:
1. **Mục tiêu (Objective)**: Tóm tắt chính xác điều cần làm.
2. **Context & Files cần đọc**: Liệt kê các file nội bộ của LogService cần đọc theo đúng ma trận đọc file tại Nguyên tắc 3.
3. **Phân tích rủi ro & Phạm vi ảnh hưởng (Impact Analysis)**:
   - Dùng GitNexus `impact` đánh giá blast radius các symbol bị ảnh hưởng.
   - Cảnh báo rõ nếu mức độ rủi ro là HIGH hoặc CRITICAL.
4. **Checklist các bước thực hiện (Step-by-Step Implementation)**: Từng bước logic, chi tiết.
5. **Kế hoạch kiểm chứng (Verification)**: `dotnet build` và `dotnet test` tương ứng.
---
## 🧭 NGUYÊN TẮC 2: QUY TẮC PHẠM VI NỘI BỘ (INWARD-ONLY / SELF-CONTAINED)
LogService là một Git Repository độc lập (`LogService.git`):
- **TUYỆT ĐỐI CHỈ ĐƯỢC trỏ tới các tài nguyên nội bộ của LogService**.
- **TUYỆT ĐỐI KHÔNG ĐƯỢC trỏ ngược ra ngoài Root** (`../../../docs/...`) hay sang các Service khác (`../../Catalog/...`).
- Toàn bộ tài liệu, hướng dẫn và cấu hình trong repo này phải 100% tự cung tự cấp (Self-contained).
---
## 📚 NGUYÊN TẮC 3: DANH MỤC FILE NỘI BỘ BẮT BUỘC PHẢI ĐỌC TẠI LOG SERVICE
Khi bắt đầu làm việc tại LogService, AI Agent **BẮT BUỘC PHẢI ĐỌC** các file nội bộ sau theo thứ tự:
1. `specs/00_AGENT_MISSION_GUARDRAILS.md`: Bounded context nhật ký hệ thống, port `8004` (Docker) / `5280`+`7103` (local), DB `5436` (`log_db`).
2. `specs/01_CLEAN_ARCHITECTURE_LAYERS.md`: Cấu trúc 4 tầng Clean Architecture, luồng phụ thuộc, vị trí DI. ⚠️ "Batching Sink hiệu năng cao" là **đặc tả mục tiêu, chưa triển khai** trong code.
3. `specs/02_API_GRPC_CONTRACTS.md`: Hợp đồng gRPC Server (`LogApi`): 4 rpc **chỉ đọc + cấu hình event type**, **không có rpc ghi log**.
4. `specs/03_EVENT_MESSAGING.md`: Consumer `ILogEventOccurred` qua RabbitMQ + MassTransit `8.3.0` — ⚠️ chưa có service nào publish event này.
5. `specs/04_DATA_PERSISTENCE.md`: Schema PostgreSQL (`Logs`, `LogEventTypes`, `CentralAuditLogs`, `AuditSyncBatches`, `AppLogFileCatalogs`, `SecurityAuditAlerts`).
6. `specs/05_DEV_VERIFICATION_CHECKLIST.md`: Lệnh build kiểm chứng service; ⚠️ hiện **không có test project** nên bước `dotnet test` không có gì để chạy.
---
## 🔒 NGUYÊN TẮC 4: RÀO CHẮN BẤT BIẾN TẠI LOG SERVICE
1. **TUYỆT ĐỐI KHÔNG SỬA ĐỔI HOẶC XÓA BẢN GHI LOG (APPEND-ONLY)**: Bảng `Logs` (entity `LogEntry`) chỉ được phép `INSERT` và `SELECT`. Cấm viết API sửa hoặc xóa log.
2. **TUYỆT ĐỐI KHÔNG DÙNG REPOSITORY PATTERN BỌC NGOÀI EF CORE**: Sử dụng trực tiếp `LogDbContext` và `DbSet<T>` (hoặc interface `IApplicationDbContext`) trong tầng Application. Cấm tạo `ILogRepository`, `UnitOfWork`.
3. **CẤU HÌNH PORT 8004 VÀ DATABASE 5436 LÀ BẤT BIẾN (NGỮ CẢNH DOCKER)**: Docker map `8004` (`Dockerfile:3-4`, `docker-compose.yml:37`). ⚠️ `dotnet run` theo `Log.Api/Properties/launchSettings.json:17,27` lại lên `5280`/`7103` — phải chạy `ASPNETCORE_URLS=http://localhost:8004` hoặc `--no-launch-profile` nếu cần khớp gateway.
4. **HỢP ĐỒNG PROTOBUF ĐỒNG BỘ 100% VỚI GATEWAY**: File hợp đồng thật nằm ở `Log.Api/Contracts/shop.proto` (364 dòng, `service LogApi` tại dòng 72-77), gắn vào build bằng `GrpcServices="Server"` (`Log.Api/Log.Api.csproj:35`).
5. **CẤM TỰ Ý SỬA ĐỔI BIẾN MÔI TRƯỜNG, SECRET VÀ KEY BÍ MẬT (MANDATORY HUMAN-IN-THE-LOOP FOR SECRETS)**:
   - **Tuyệt đối CẤM mọi AI Coding Agent tự ý sửa đổi, thay thế, xóa hoặc ghi đè các biến môi trường (`.env`, `.env.*`), Connection Strings, API Keys, JWT Secrets, chứng chỉ hoặc bất kỳ thông tin bí mật (credentials/secrets) nào trong toàn bộ hệ thống.**
   - **Bắt buộc phải xin ý kiến và nhận được sự đồng ý rõ ràng (explicit approval) từ người dùng trước khi thực hiện bất kỳ thay đổi nào liên quan đến biến môi trường hoặc tài khoản bí mật.**
   - Khi phát hiện cấu hình môi trường, connection string hoặc secret key có vấn đề hoặc cần cập nhật, Agent **CHỈ ĐƯỢC PHÉP báo cáo hiện trạng, đề xuất phương án và chờ người dùng xác nhận**, tuyệt đối không tự ý can thiệp.
---
## 💬 NGUYÊN TẮC 5: QUY CHUẨN THÔNG BÁO LỖI TOÀN HỆ THỐNG (MANDATORY ERROR RULES)
Tất cả các thông báo lỗi (Error Messages, Exceptions, ProblemDetails, Validation Errors, Response Details) **BẮT BUỘC** phải tuân theo 3 nguyên tắc sau:
1. **Ngôn ngữ (100% English)**: Toàn bộ thông báo lỗi phải được viết bằng tiếng Anh. Tuyệt đối không dùng tiếng Việt trong chuỗi thông báo lỗi trả về cho client.
2. **Quy cách viết hoa (Sentence case)**: Viết hoa duy nhất chữ cái đầu câu, các từ tiếp theo viết thường (ngoại trừ tên riêng, từ viết tắt hoặc định danh kỹ thuật như `ID`, `OTP`, `JWT`, `URL`, `UUID`, `Email`...).
   - ✅ Đúng: `"Log entry not found with the specified ID."`
   - ✅ Đúng: `"Event type is currently disabled for logging."`
   - ❌ Sai: `"Không tìm thấy log"` (Không dùng tiếng Việt)
   - ❌ Sai: `"log entry not found"` (Chữ cái đầu không viết hoa)
   - ❌ Sai: `"LOG ENTRY NOT FOUND"` (Cấm viết hoa toàn bộ)
   - ❌ Sai: `"Log Entry Not Found Provided"` (Không dùng Title Case)
3. **Mã lỗi (Error Codes — SCREAMING_SNAKE_CASE)**: Mã lỗi luôn viết hoa toàn bộ và nối bằng dấu gạch dưới:
   - Ví dụ: `LOG_NOT_FOUND`, `EVENT_TYPE_DISABLED`, `VAL_INVALID_INPUT`, `UNAUTHORIZED`, `FORBIDDEN`.
---
## 🎯 NGUYÊN TẮC 6: ĐIỀU KIỆN HOÀN THÀNH NHIỆM VỤ (DEFINITION OF DONE)
Một task tại LogService chỉ hoàn tất khi:
- [ ] Đã lập Plan chi tiết trước khi thực hiện.
- [ ] Không chứa bất kỳ liên kết trỏ ngược ra ngoài LogService.
- [ ] `dotnet build Log.sln -c Release` thành công 100% (0 error). ⚠️ Tên solution thật là `Log.sln` (4 project: `Log.Api`, `Log.Application`, `Log.Domain`, `Log.Infrastructure`) — **không có** `LogService.sln`.
- [ ] ⚠️ `dotnet test Log.sln -c Release`: hiện **repo chưa có test project nào** (0/0), nên bước này chưa kiểm chứng được gì. Khi bổ sung test project (khuyến nghị `Log.UnitTests` + `Log.IntegrationTests`) thì bắt buộc phải xanh 100%.
- [ ] Trạng thái Git sạch, không commit file rác (`bin/`, `obj/`, `.vs/`, `.idea/`).
---
## 📌 NGUYÊN TẮC 7: TRẠNG THÁI TRIỂN KHAI THỰC TẾ (SNAPSHOT 2026-10-24)
Bảng dưới đây là **sự thật đã kiểm chứng bằng code**, dùng để chống ảo giác khi lập plan. Không được mô tả các mục ❌ như đã có.
| # | Hạng mục | Trạng thái | Bằng chứng |
|---|----------|------------|------------|
| 1 | gRPC Server `LogApi` (4/4 rpc) | ✅ CÓ | `Log.Api/Services/LogGrpcService.cs:22,67,99,119`; map tại `Log.Api/Program.cs:47`; proto `Log.Api/Contracts/shop.proto:72-77` |
| 2 | OpenTelemetry (trace/metric/log + OTLP) | ✅ CÓ | `Log.Api/Extensions/ObservabilityExtensions.cs:8-25`, gọi tại `Log.Api/Program.cs:12` |
| 3 | Swagger UI + gRPC reflection | ✅ CÓ | `Log.Api/Program.cs:37-41`; `:23` `AddGrpcReflection`, `:41` `MapGrpcReflectionService` |
| 4 | REST `POST /api/v1/log/batch` | ❌ CHƯA CÓ | `Log.Api/Program.cs` chỉ map `GET /health` (`:66`) và `GET /` (`:67`); 0 controller, 0 `MapPost` |
| 5 | Đường ghi log qua RabbitMQ (`ILogEventOccurred`) | ⚠️ MỘT PHẦN | Consumer có (`Log.Application/Consumers/GlobalLogConsumer.cs:10`, đăng ký `Log.Infrastructure/InfrastructureDependencyInjection.cs:34`) nhưng **0 publisher** trong toàn bộ `src/` → consumer là dead code |
| 6 | 5 index hiệu năng trên bảng `Logs` | ❌ CHƯA CÓ | `0/5`. `LogEntryConfiguration` không có `HasIndex`; migration `Log.Infrastructure/Migrations/20261024103313_InitialLogSchema.cs` có 8 `CreateIndex` nhưng **không cái nào trên `Logs`** |
| 7 | Test project | ❌ CHƯA CÓ | 0 project test trong `Log.sln`; CI `.github/workflows/ci-cd.yml` chỉ build/push Docker |
| 8 | Thông báo lỗi 100% English (Nguyên tắc 5) | ❌ CHƯA ĐẠT | 5 chuỗi tiếng Việt: `LogGrpcService.cs:70,74,122`; `SetLogEventTypeEnabledCommand.cs:16,41` |
| 9 | Batching Sink hiệu năng cao | ❌ CHƯA CÓ | Không có class/queue/buffer nào; chỉ có consumer ghi từng message một |
| 10 | Port khớp hợp đồng `8004` | ⚠️ MỘT PHẦN | Docker đúng `8004` (`Dockerfile:3-4`, `docker-compose.yml:37`); local `Log.Api/Properties/launchSettings.json:17,27` là `5280`/`7103` |
| 11 | Repo tự cung tự cấp (không phụ thuộc ngoài) | ⚠️ MỘT PHẦN | `Log.Domain.csproj:10` kéo `MediatR.Contracts 2.0.1`; `Log.Application.csproj:8-15` kéo MassTransit/FluentValidation/EFCore |
| 12 | Migration + 9 bảng PostgreSQL | ✅ CÓ | 1 migration `20261024103313_InitialLogSchema`; 6 bảng nghiệp vụ + 3 bảng MassTransit (`InboxState`, `OutboxMessage`, `OutboxState`) |