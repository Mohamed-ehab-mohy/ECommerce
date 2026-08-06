using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    on_hand = table.Column<int>(type: "integer", nullable: false),
                    allocated = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_items_warehouses",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    on_hand_delta = table.Column<int>(type: "integer", nullable: false),
                    allocated_delta = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_stock_items",
                        column: x => x.stock_item_id,
                        principalTable: "stock_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_items_warehouse_id",
                table: "stock_items",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ux_stock_items_sku_warehouse",
                table: "stock_items",
                columns: new[] { "sku", "warehouse_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_stock_item_id",
                table: "stock_movements",
                column: "stock_item_id");

            migrationBuilder.Sql(
                "CREATE OR REPLACE FUNCTION fn_reject_stock_movements_change()\n" +
                "RETURNS trigger AS $stock$\n" +
                "BEGIN\n" +
                "    RAISE EXCEPTION 'stock_movements is append-only; updates and deletes are not allowed.';\n" +
                "END;\n" +
                "$stock$ LANGUAGE plpgsql;\n" +
                "\n" +
                "CREATE TRIGGER trg_stock_movements_append_only\n" +
                "BEFORE UPDATE OR DELETE ON stock_movements\n" +
                "FOR EACH ROW EXECUTE FUNCTION fn_reject_stock_movements_change();");

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: ["role_id", "permission_code"],
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), "inventory.stock.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "inventory.stock.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "inventory.stock.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "inventory.stock.write" }
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
                    { new Guid("44444444-4444-4444-4444-444444444444"), "inventory.stock.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "inventory.stock.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "inventory.stock.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "inventory.stock.write" }
                });

            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS trg_stock_movements_append_only ON stock_movements;\n" +
                "DROP FUNCTION IF EXISTS fn_reject_stock_movements_change();");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "stock_items");
        }
    }
}
