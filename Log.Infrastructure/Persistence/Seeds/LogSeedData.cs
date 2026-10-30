namespace Log.Infrastructure.Persistence.Seeds;

using Microsoft.EntityFrameworkCore;
using Log.Domain.Entities;
using Log.Domain.Enums;

public static class LogSeedData
{
    public static void SeedAll(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var adminUserId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
        var buyerUserId = Guid.Parse("dddddddd-4444-4444-4444-444444444444");

        // 1. Registered Event Types across microservices
        modelBuilder.Entity<LogEventType>().HasData(
            new LogEventType
            {
                Code = "USER_REGISTER",
                ServiceName = "User",
                Category = "Authentication",
                Description = "Đăng ký tài khoản người dùng mới",
                IsEnabled = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new LogEventType
            {
                Code = "SELLER_APPROVE",
                ServiceName = "User",
                Category = "Onboarding",
                Description = "Phê duyệt hồ sơ người bán hàng",
                IsEnabled = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new LogEventType
            {
                Code = "ORDER_PAID",
                ServiceName = "Commerce",
                Category = "Order",
                Description = "Thanh toán đơn hàng thành công qua ví điện tử",
                IsEnabled = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new LogEventType
            {
                Code = "AUCTION_BID",
                ServiceName = "Auction",
                Category = "Auction",
                Description = "Khách hàng đặt giá thầu trong phiên đấu giá",
                IsEnabled = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new LogEventType
            {
                Code = "SHIPMENT_DELIVERED",
                ServiceName = "Fulfillment",
                Category = "Shipment",
                Description = "Giao hàng thành công tới người mua",
                IsEnabled = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );

        // 2. Sample Central Audit Logs (Linked to real activities across services)
        modelBuilder.Entity<CentralAuditLog>().HasData(
            new CentralAuditLog
            {
                Id = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                ServiceName = "User",
                TraceId = "trace-user-approve-seller-001",
                UserId = adminUserId.ToString(),
                UserName = "admin@nexus.com",
                Action = CentralAuditAction.Approve,
                EntityName = "SellerProfile",
                EntityId = "cccccccc-3333-3333-3333-333333333334",
                OldValues = "{\"status\":\"Pending\"}",
                NewValues = "{\"status\":\"Approved\"}",
                ChangedColumns = "[\"Status\"]",
                IpAddress = "127.0.0.1",
                UserAgent = "NexusAdminPortal/1.0",
                SyncedAt = seedDate,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            },
            new CentralAuditLog
            {
                Id = Guid.Parse("90000000-0000-0000-0000-000000000002"),
                ServiceName = "Commerce",
                TraceId = "trace-order-paid-001",
                UserId = buyerUserId.ToString(),
                UserName = "buyer@nexus.com",
                Action = CentralAuditAction.Create,
                EntityName = "SubOrder",
                EntityId = "02000000-0000-0000-0000-000000000001",
                OldValues = null,
                NewValues = "{\"subOrderNo\":\"SUB-20260101-0001\",\"totalAmount\":850000}",
                ChangedColumns = null,
                IpAddress = "127.0.0.1",
                UserAgent = "NexusApp/1.0",
                SyncedAt = seedDate,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );

        // 3. Operational Log Entries
        modelBuilder.Entity<LogEntry>().HasData(
            new LogEntry
            {
                Id = Guid.Parse("91000000-0000-0000-0000-000000000001"),
                ServiceName = "Commerce",
                EventType = "ORDER_PAID",
                Action = "PAYMENT_COMPLETED",
                EntityName = "SubOrder",
                EntityId = "02000000-0000-0000-0000-000000000001",
                ActorUserId = buyerUserId.ToString(),
                ActorUsername = "buyer@nexus.com",
                ActorRole = "Buyer",
                CorrelationId = "corr-order-001",
                CausationId = "cause-order-001",
                MessageId = "msg-order-001",
                SchemaVersion = 1,
                TraceId = "trace-order-paid-001",
                Payload = "{\"amount\":850000,\"currency\":\"VND\",\"subOrderNo\":\"SUB-20260101-0001\"}",
                LogLevel = "INFO",
                OccurredAt = seedDate,
                RecordedAt = seedDate
            },
            new LogEntry
            {
                Id = Guid.Parse("91000000-0000-0000-0000-000000000002"),
                ServiceName = "User",
                EventType = "USER_REGISTER",
                Action = "ACCOUNT_CREATED",
                EntityName = "User",
                EntityId = buyerUserId.ToString(),
                ActorUserId = buyerUserId.ToString(),
                ActorUsername = "buyer@nexus.com",
                ActorRole = "Buyer",
                CorrelationId = "corr-user-reg-001",
                CausationId = "cause-user-reg-001",
                MessageId = "msg-user-reg-001",
                SchemaVersion = 1,
                TraceId = "trace-user-reg-001",
                Payload = "{\"email\":\"buyer@nexus.com\",\"role\":\"Buyer\"}",
                LogLevel = "INFO",
                OccurredAt = seedDate,
                RecordedAt = seedDate
            }
        );

        // 4. Audit Sync Batch
        modelBuilder.Entity<AuditSyncBatch>().HasData(
            new AuditSyncBatch
            {
                Id = Guid.Parse("92000000-0000-0000-0000-000000000001"),
                ServiceName = "Commerce",
                BatchSize = 10,
                FirstRecordId = "rec-001",
                LastRecordId = "rec-010",
                Status = AuditSyncStatus.Completed,
                StartedAt = seedDate,
                CompletedAt = seedDate.AddMinutes(1),
                ErrorMessage = null,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );

        // 5. App Log File Catalog
        modelBuilder.Entity<AppLogFileCatalog>().HasData(
            new AppLogFileCatalog
            {
                Id = Guid.Parse("93000000-0000-0000-0000-000000000001"),
                ServiceName = "ApiGateway",
                Environment = AppEnvironment.Production,
                LogDate = seedDate,
                FilePath = "/var/log/nexus/gateway-2026-01-01.log",
                FileSizeBytes = 1048576,
                Checksum = "sha256-abc123def456",
                TotalLines = 5000,
                ErrorCount = 0,
                WarningCount = 2,
                IsActive = true,
                IsDisabled = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );

        // 6. Security Audit Alert
        modelBuilder.Entity<SecurityAuditAlert>().HasData(
            new SecurityAuditAlert
            {
                Id = Guid.Parse("94000000-0000-0000-0000-000000000001"),
                AlertTitle = "Xét duyệt người bán hàng đặc cách",
                Severity = AlertSeverity.Low,
                ServiceName = "User",
                CentralAuditLogId = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                TriggeredRule = SecurityRuleType.PrivilegeEscalation,
                Description = "Ghi nhận nâng cấp quyền người bán trong đợt khởi tạo",
                Status = AlertStatus.Resolved,
                ResolvedBy = adminUserId,
                ResolvedAt = seedDate.AddHours(1),
                ResolutionNote = "Thao tác hợp lệ bởi Administrator.",
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false
            }
        );
    }
}
