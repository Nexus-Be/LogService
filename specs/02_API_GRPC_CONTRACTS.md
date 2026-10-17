# 02 — HỢP ĐỒNG GIAO TIẾP GRPC (LOG SERVICE GRPC CONTRACTS)

Dịch vụ gRPC của LogService được định nghĩa trong hợp đồng duy nhất `Contracts/shop.proto` dưới service `LogApi`:

---

## 1. DANH SÁCH CÁC RPC METHODS

```protobuf
service LogApi {
  // Tra cứu danh sách nhật ký có phân trang và bộ lọc nâng cao
  rpc QueryLogs(QueryLogsRequest) returns (LogPage);
  
  // Lấy chi tiết một bản ghi nhật ký (kèm đầy đủ Payload JSON)
  rpc GetLog(IdRequest) returns (LogEntry);
  
  // Lấy danh mục các loại sự kiện log
  rpc ListLogEventTypes(Empty) returns (LogEventTypeList);
  
  // Bật/tắt ghi log cho một loại sự kiện cụ thể
  rpc SetLogEventTypeEnabled(SetLogEventTypeEnabledRequest) returns (LogEventType);
}
```

---

## 2. CÁC MESSAGE CONTRACTS LIÊN QUAN

```protobuf
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

message LogPage { 
  repeated LogEntry items = 1; 
  int32 total = 2; 
}

message QueryLogsRequest {
  int32 page = 1; 
  int32 page_size = 2;
  string from = 3; 
  string to = 4;
  string service_name = 5; 
  string event_type = 6;
  string entity_name = 7; 
  string entity_id = 8;
  string actor_user_id = 9; 
  string correlation_id = 10;
  string log_level = 11;
}

message LogEventType {
  string code = 1; 
  string service_name = 2; 
  string category = 3; 
  string description = 4; 
  bool is_enabled = 5;
}

message LogEventTypeList { 
  repeated LogEventType items = 1; 
  int32 total = 2; 
}

message SetLogEventTypeEnabledRequest { 
  string code = 1; 
  bool enabled = 2; 
}
```
