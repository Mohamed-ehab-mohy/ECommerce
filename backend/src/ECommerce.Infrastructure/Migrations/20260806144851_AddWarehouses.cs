using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_warehouses_code",
                table: "warehouses",
                column: "code",
                unique: true);

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: ["role_id", "permission_code"],
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), "inventory.warehouse.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "inventory.warehouse.write" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "inventory.warehouse.delete" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "inventory.warehouse.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "inventory.warehouse.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "inventory.warehouse.delete" }
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
                    { new Guid("44444444-4444-4444-4444-444444444444"), "inventory.warehouse.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "inventory.warehouse.write" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "inventory.warehouse.delete" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "inventory.warehouse.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "inventory.warehouse.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "inventory.warehouse.delete" }
                });

            migrationBuilder.DropTable(
                name: "warehouses");
        }
    }
}
