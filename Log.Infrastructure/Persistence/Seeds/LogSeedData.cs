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
    }
}
