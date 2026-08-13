using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBackorder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "backorderable",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "order_backorder_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    filled_quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    filled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_backorder_items", x => x.id);
                    table.CheckConstraint("ck_order_backorder_items_filled_range", "\"filled_quantity\" BETWEEN 0 AND 99");
                    table.CheckConstraint("ck_order_backorder_items_quantity_range", "\"quantity\" BETWEEN 1 AND 99");
                    table.ForeignKey(
                        name: "fk_order_backorder_items_orders",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_backorder_items_order_id",
                table: "order_backorder_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_backorder_items_sku_status",
                table: "order_backorder_items",
                columns: new[] { "sku", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_backorder_items");

            migrationBuilder.DropColumn(
                name: "backorderable",
                table: "products");
        }
    }
}
