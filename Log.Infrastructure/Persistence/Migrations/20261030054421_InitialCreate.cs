using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Log.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppLogFileCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    LogDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TotalLines = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    WarningCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLogFileCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditSyncBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BatchSize = table.Column<int>(type: "integer", nullable: false),
                    FirstRecordId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastRecordId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditSyncBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CentralAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    ChangedColumns = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentralAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogEventTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEventTypes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActorUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CausationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    LogLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertTitle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CentralAuditLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggeredRule = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAuditAlerts_CentralAuditLogs_CentralAuditLogId",
                        column: x => x.CentralAuditLogId,
                        principalTable: "CentralAuditLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppLogFileCatalogs",
                columns: new[] { "Id", "Checksum", "CreatedAt", "DeletedAt", "Environment", "ErrorCount", "FilePath", "FileSizeBytes", "IsActive", "IsDeleted", "IsDisabled", "LogDate", "ServiceName", "TotalLines", "UpdatedAt", "WarningCount" },
                values: new object[] { new Guid("93000000-0000-0000-0000-000000000001"), "sha256-abc123def456", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2, 0, "/var/log/nexus/gateway-2026-01-01.log", 1048576L, true, false, false, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ApiGateway", 5000, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2 });

            migrationBuilder.InsertData(
                table: "AuditSyncBatches",
                columns: new[] { "Id", "BatchSize", "CompletedAt", "CreatedAt", "DeletedAt", "ErrorMessage", "FirstRecordId", "IsDeleted", "LastRecordId", "ServiceName", "StartedAt", "Status", "UpdatedAt" },
                values: new object[] { new Guid("92000000-0000-0000-0000-000000000001"), 10, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 1, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "rec-001", false, "rec-010", "Commerce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "CentralAuditLogs",
                columns: new[] { "Id", "Action", "ChangedColumns", "CreatedAt", "DeletedAt", "EntityId", "EntityName", "IpAddress", "IsDeleted", "NewValues", "OldValues", "ServiceName", "SyncedAt", "TraceId", "UpdatedAt", "UserAgent", "UserId", "UserName" },
                values: new object[,]
                {
                    { new Guid("90000000-0000-0000-0000-000000000001"), 5, "[\"Status\"]", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "cccccccc-3333-3333-3333-333333333334", "SellerProfile", "127.0.0.1", false, "{\"status\":\"Approved\"}", "{\"status\":\"Pending\"}", "User", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "trace-user-approve-seller-001", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "NexusAdminPortal/1.0", "aaaaaaaa-1111-1111-1111-111111111111", "admin@nexus.com" },
                    { new Guid("90000000-0000-0000-0000-000000000002"), 0, null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "02000000-0000-0000-0000-000000000001", "SubOrder", "127.0.0.1", false, "{\"subOrderNo\":\"SUB-20260101-0001\",\"totalAmount\":850000}", null, "Commerce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "trace-order-paid-001", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "NexusApp/1.0", "dddddddd-4444-4444-4444-444444444444", "buyer@nexus.com" }
                });

            migrationBuilder.InsertData(
                table: "LogEventTypes",
                columns: new[] { "Code", "Category", "CreatedAt", "Description", "IsEnabled", "ServiceName", "UpdatedAt" },
                values: new object[,]
                {
                    { "AUCTION_BID", "Auction", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Khách hàng đặt giá thầu trong phiên đấu giá", true, "Auction", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "ORDER_PAID", "Order", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Thanh toán đơn hàng thành công qua ví điện tử", true, "Commerce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "SELLER_APPROVE", "Onboarding", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Phê duyệt hồ sơ người bán hàng", true, "User", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "SHIPMENT_DELIVERED", "Shipment", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Giao hàng thành công tới người mua", true, "Fulfillment", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "USER_REGISTER", "Authentication", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Đăng ký tài khoản người dùng mới", true, "User", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "Logs",
                columns: new[] { "Id", "Action", "ActorRole", "ActorUserId", "ActorUsername", "CausationId", "CorrelationId", "CreatedAt", "DeletedAt", "EntityId", "EntityName", "EventType", "IsDeleted", "LogLevel", "MessageId", "OccurredAt", "Payload", "RecordedAt", "SchemaVersion", "ServiceName", "TraceId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("91000000-0000-0000-0000-000000000001"), "PAYMENT_COMPLETED", "Buyer", "dddddddd-4444-4444-4444-444444444444", "buyer@nexus.com", "cause-order-001", "corr-order-001", new DateTimeOffset(new DateTime(2026, 10, 30, 5, 44, 21, 94, DateTimeKind.Unspecified).AddTicks(8355), new TimeSpan(0, 0, 0, 0, 0)), null, "02000000-0000-0000-0000-000000000001", "SubOrder", "ORDER_PAID", false, "INFO", "msg-order-001", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "{\"amount\":850000,\"currency\":\"VND\",\"subOrderNo\":\"SUB-20260101-0001\"}", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Commerce", "trace-order-paid-001", new DateTimeOffset(new DateTime(2026, 10, 30, 5, 44, 21, 94, DateTimeKind.Unspecified).AddTicks(8355), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("91000000-0000-0000-0000-000000000002"), "ACCOUNT_CREATED", "Buyer", "dddddddd-4444-4444-4444-444444444444", "buyer@nexus.com", "cause-user-reg-001", "corr-user-reg-001", new DateTimeOffset(new DateTime(2026, 10, 30, 5, 44, 21, 94, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), null, "dddddddd-4444-4444-4444-444444444444", "User", "USER_REGISTER", false, "INFO", "msg-user-reg-001", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "{\"email\":\"buyer@nexus.com\",\"role\":\"Buyer\"}", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "User", "trace-user-reg-001", new DateTimeOffset(new DateTime(2026, 10, 30, 5, 44, 21, 94, DateTimeKind.Unspecified).AddTicks(8364), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "SecurityAuditAlerts",
                columns: new[] { "Id", "AlertTitle", "CentralAuditLogId", "CreatedAt", "DeletedAt", "Description", "IsDeleted", "ResolutionNote", "ResolvedAt", "ResolvedBy", "ServiceName", "Severity", "Status", "TriggeredRule", "UpdatedAt" },
                values: new object[] { new Guid("94000000-0000-0000-0000-000000000001"), "Xét duyệt người bán hàng đặc cách", new Guid("90000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Ghi nhận nâng cấp quyền người bán trong đợt khởi tạo", false, "Thao tác hợp lệ bởi Administrator.", new DateTimeOffset(new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("aaaaaaaa-1111-1111-1111-111111111111"), "User", 0, 2, 2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_AppLogFileCatalogs_ServiceName_LogDate",
                table: "AppLogFileCatalogs",
                columns: new[] { "ServiceName", "LogDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditAlerts_CentralAuditLogId",
                table: "SecurityAuditAlerts",
                column: "CentralAuditLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLogFileCatalogs");

            migrationBuilder.DropTable(
                name: "AuditSyncBatches");

            migrationBuilder.DropTable(
                name: "LogEventTypes");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "SecurityAuditAlerts");

            migrationBuilder.DropTable(
                name: "CentralAuditLogs");
        }
    }
}
