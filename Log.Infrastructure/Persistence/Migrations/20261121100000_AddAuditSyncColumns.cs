using Log.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Log.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Lets the central audit log remember where each record came from, so pulling the same record twice stores it once.
    /// Idempotent: safe on a fresh database and on one that already has these columns.
    /// </summary>
    [DbContext(typeof(LogDbContext))]
    [Migration("20261121100000_AddAuditSyncColumns")]
    public partial class AddAuditSyncColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""CentralAuditLogs"" ADD COLUMN IF NOT EXISTS ""SourceRecordId"" uuid NULL;
ALTER TABLE ""CentralAuditLogs"" ADD COLUMN IF NOT EXISTS ""ActionName"" character varying(100) NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CentralAuditLogs_ServiceName_SourceRecordId"" ON ""CentralAuditLogs"" (""ServiceName"", ""SourceRecordId"") WHERE ""SourceRecordId"" IS NOT NULL;
CREATE INDEX IF NOT EXISTS ""IX_CentralAuditLogs_CreatedAt"" ON ""CentralAuditLogs"" (""CreatedAt"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SecurityAuditAlerts_Log_Rule"" ON ""SecurityAuditAlerts"" (""CentralAuditLogId"", ""TriggeredRule"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_SecurityAuditAlerts_Log_Rule"";
DROP INDEX IF EXISTS ""IX_CentralAuditLogs_CreatedAt"";
DROP INDEX IF EXISTS ""IX_CentralAuditLogs_ServiceName_SourceRecordId"";
ALTER TABLE ""CentralAuditLogs"" DROP COLUMN IF EXISTS ""ActionName"";
ALTER TABLE ""CentralAuditLogs"" DROP COLUMN IF EXISTS ""SourceRecordId"";");
        }
    }
}
