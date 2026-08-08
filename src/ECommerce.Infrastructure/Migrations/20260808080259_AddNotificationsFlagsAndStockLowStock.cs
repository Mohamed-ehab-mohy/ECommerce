using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsFlagsAndStockLowStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "low_stock_cooldown",
                table: "stock_items",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(1, 0, 0, 0, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "low_stock_notified_at",
                table: "stock_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "low_stock_threshold",
                table: "stock_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "feature_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_feature_flags_key",
                table: "feature_flags",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_notification_preferences_customer_channel_kind",
                table: "notification_preferences",
                columns: new[] { "customer_id", "channel", "kind" },
                unique: true);

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: ["role_id", "permission_code"],
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), "platform.flags.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "platform.flags.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "platform.flags.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "platform.flags.write" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: ["role_id", "permission_code"],
                keyValues: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), "platform.flags.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "platform.flags.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "platform.flags.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "platform.flags.write" }
                });

            migrationBuilder.DropTable(
                name: "feature_flags");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "low_stock_cooldown",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "low_stock_notified_at",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "low_stock_threshold",
                table: "stock_items");
        }
    }
}
