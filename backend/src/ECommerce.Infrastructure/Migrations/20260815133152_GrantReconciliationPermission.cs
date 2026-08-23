using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GrantReconciliationPermission : Migration
    {
        private static readonly Guid Finance = new("33333333-3333-3333-3333-333333333333");
        private static readonly Guid Admin = new("44444444-4444-4444-4444-444444444444");
        private static readonly Guid SuperAdmin = new("55555555-5555-5555-5555-555555555555");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: ["role_id", "permission_code"],
                values: new object[,]
                {
                    { Finance, "finance.reconcile" },
                    { Admin, "finance.reconcile" },
                    { SuperAdmin, "finance.reconcile" }
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
                    { Finance, "finance.reconcile" },
                    { Admin, "finance.reconcile" },
                    { SuperAdmin, "finance.reconcile" }
                });
        }
    }
}
