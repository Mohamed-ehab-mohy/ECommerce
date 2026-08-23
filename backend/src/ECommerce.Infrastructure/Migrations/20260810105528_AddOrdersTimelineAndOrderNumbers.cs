using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersTimelineAndOrderNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SEQUENCE order_number_seq START WITH 1 INCREMENT BY 1 NO CYCLE;");

            migrationBuilder.AddColumn<string>(
                name: "order_number",
                table: "orders",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "order_status_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    to_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    actor_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_status_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_status_log_orders",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer_id_placed_at",
                table: "orders",
                columns: new[] { "customer_id", "placed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_placed_at",
                table: "orders",
                column: "placed_at");

            migrationBuilder.CreateIndex(
                name: "ix_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_orders_order_number",
                table: "orders",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_status_log_order_id",
                table: "order_status_log",
                column: "order_id");

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: ["role_id", "permission_code"],
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), "orders.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "orders.support.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "orders.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "orders.support.read" }
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
                    { new Guid("44444444-4444-4444-4444-444444444444"), "orders.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "orders.support.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "orders.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "orders.support.read" }
                });

            migrationBuilder.DropTable(
                name: "order_status_log");

            migrationBuilder.DropIndex(
                name: "ix_orders_customer_id_placed_at",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_placed_at",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_status",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ux_orders_order_number",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_number",
                table: "orders");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS order_number_seq;");
        }
    }
}
