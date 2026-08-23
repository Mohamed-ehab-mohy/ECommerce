using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsAndWebhooks : Migration
    {
        private static readonly Guid Finance = new("33333333-3333-3333-3333-333333333333");
        private static readonly Guid Admin = new("44444444-4444-4444-4444-444444444444");
        private static readonly Guid SuperAdmin = new("55555555-5555-5555-5555-555555555555");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "export_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    filters_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    row_count = table.Column<int>(type: "integer", nullable: false),
                    file_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_retry_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_status_code = table.Column<int>(type: "integer", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    delivered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_deliveries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_endpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    suspended_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    secret_rotated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    event_types = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_endpoints", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_ledger_occurred_at_event_type",
                table: "payment_ledger",
                columns: new[] { "occurred_at", "event_type" });

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_status",
                table: "export_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_deliveries_endpoint_id",
                table: "webhook_deliveries",
                column: "endpoint_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_deliveries_event_id",
                table: "webhook_deliveries",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_deliveries_status_next_retry",
                table: "webhook_deliveries",
                columns: new[] { "status", "next_retry_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoints_is_active",
                table: "webhook_endpoints",
                column: "is_active");

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: ["role_id", "permission_code"],
                values: new object[,]
                {
                    { Finance, "reports.read" },
                    { Admin, "reports.read" },
                    { SuperAdmin, "reports.read" },
                    { Admin, "integrations.read" },
                    { SuperAdmin, "integrations.read" },
                    { Admin, "integrations.write" },
                    { SuperAdmin, "integrations.write" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "export_jobs");

            migrationBuilder.DropTable(
                name: "webhook_deliveries");

            migrationBuilder.DropTable(
                name: "webhook_endpoints");

            migrationBuilder.DropIndex(
                name: "ix_payment_ledger_occurred_at_event_type",
                table: "payment_ledger");

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: ["role_id", "permission_code"],
                keyValues: new object[,]
                {
                    { Finance, "reports.read" },
                    { Admin, "reports.read" },
                    { SuperAdmin, "reports.read" },
                    { Admin, "integrations.read" },
                    { SuperAdmin, "integrations.read" },
                    { Admin, "integrations.write" },
                    { SuperAdmin, "integrations.write" }
                });
        }
    }
}
