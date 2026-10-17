# 05 — DANH SÁCH KIỂM CHỨNG & NGHIỆM THU (LOG SERVICE CHECKLIST)

---

## 1. LỆNH BIÊN DỊCH & CHẠY THỬ NGHIỆM

```bash
# 1. Biên dịch solution Log
dotnet build Log.sln -c Release

# 2. Khởi chạy service (gRPC port 8004)
dotnet run --project Log.Api

# 3. Thử nghiệm qua ApiGateway:
# Mở gateway.runtime.json, kiểm tra "log": { "Mode": "Real", "Address": "http://localhost:8004" }
# Gọi GET /api/v1/log/event-types trên Swagger Gateway xem kết quả thật trả về.
```

---

## 2. CHECKLIST TRƯỚC KHI COMMIT CHO AI AGENTS

- [ ] **Biên dịch thành công 100%**: Không lỗi cảnh báo nghiêm trọng.
- [ ] **Tính bất biến (Append-Only)**: Không có hàm hoặc endpoint nào cho phép UPDATE hay DELETE `LogEntry`.
- [ ] **Port & Database**: Lắng nghe đúng port `8004`, kết nối `log_db` trên port `5435`.
- [ ] **No-Repository Pattern**: Tương tác trực tiếp qua DbContext / DbSet.
- [ ] **Git Status**: Không commit file rác (`bin/`, `obj/`, `.idea/`, `.env`).
