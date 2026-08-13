using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFulfillmentAndShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fulfillment_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    packed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    shipped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fulfillment_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_fulfillment_tasks_orders",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fulfillment_task_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    bin_location = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fulfillment_task_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_fulfillment_task_items_tasks",
                        column: x => x.task_id,
                        principalTable: "fulfillment_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    carrier_key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tracking_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    shipped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.id);
                    table.ForeignKey(
                        name: "fk_shipments_fulfillment_tasks",
                        column: x => x.fulfillment_task_id,
                        principalTable: "fulfillment_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shipments_orders",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tracking_updates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracking_updates", x => x.id);
                    table.ForeignKey(
                        name: "fk_tracking_updates_shipments",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fulfillment_task_items_task_id",
                table: "fulfillment_task_items",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ix_fulfillment_tasks_warehouse_status",
                table: "fulfillment_tasks",
                columns: new[] { "warehouse_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_fulfillment_tasks_order_id",
                table: "fulfillment_tasks",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipments_fulfillment_task_id",
                table: "shipments",
                column: "fulfillment_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_order_id",
                table: "shipments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ux_shipments_tracking_number",
                table: "shipments",
                column: "tracking_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tracking_updates_shipment_id",
                table: "tracking_updates",
                column: "shipment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fulfillment_task_items");

            migrationBuilder.DropTable(
                name: "tracking_updates");

            migrationBuilder.DropTable(
                name: "shipments");

            migrationBuilder.DropTable(
                name: "fulfillment_tasks");
        }
    }
}
