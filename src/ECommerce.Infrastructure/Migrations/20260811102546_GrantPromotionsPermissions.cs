using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GrantPromotionsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: ["role_id", "permission_code"],
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), "promotions.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "promotions.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "promotions.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "promotions.write" }
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
                    { new Guid("44444444-4444-4444-4444-444444444444"), "promotions.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "promotions.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "promotions.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "promotions.write" }
                });
        }
    }
}
