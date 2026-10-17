# 01 — PHÂN TẦNG CLEAN ARCHITECTURE & QUY TẮC PHỤ THUỘC (LOG SERVICE)

Solution `Log.sln` bao gồm 4 project tương ứng với 4 tầng kiến trúc sạch (Clean Architecture):

```
Log.sln
├── Log.Domain/           # Entities (LogEntry, LogEventType), Events (ILogEventOccurred)
├── Log.Application/      # Use cases CQRS (QueryLogsQuery, GetLogQuery, SetLogEventTypeEnabledCommand, GlobalLogConsumer)
├── Log.Infrastructure/   # EF Core DbContext, PostgreSQL mappings, JSONB storage, MassTransit Outbox
└── Log.Api/              # Điểm khởi chạy gRPC Server (Program.cs, DI setup, OpenTelemetry)
```

---

## 1. NGUYÊN TẮC LUỒNG PHỤ THUỘC (DEPENDENCY RULE)

```
[ Log.Api ] ─────────────┐
                         │
                         ▼
             [ Log.Application ] ──▶ [ Log.Domain ]
                         ▲
                         │
[ Log.Infrastructure ] ──┘
```

- **`Log.Domain`**: Chứa Entity `LogEntry` và `LogEventType`. Độc lập 100%, không phụ thuộc tầng nào khác.
- **`Log.Application`**: Chứa interface `IApplicationDbContext`, các CQRS Queries/Commands (`QueryLogsQuery`, `GetLogQuery`, `ListLogEventTypesQuery`, `SetLogEventTypeEnabledCommand`), và MassTransit Consumer `GlobalLogConsumer`.
- **`Log.Infrastructure`**: Chứa `LogDbContext`, cấu hình EntityTypeConfiguration Fluent API, ánh xạ cột `Payload` sang kiểu native `jsonb` của PostgreSQL để tối ưu dung lượng và tìm kiếm nhanh.
- **`Log.Api`**: Điểm khởi động gRPC Server, đăng ký `app.MapGrpcService<LogGrpcService>()`, tích hợp OpenTelemetry Distributed Tracing và Swagger.
