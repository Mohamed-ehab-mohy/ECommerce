using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GrantReviewsPermission : Migration
    {
        private static readonly Guid Staff = new("22222222-2222-2222-2222-222222222222");
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
                    { Staff, "reviews.moderate" },
                    { Admin, "reviews.moderate" },
                    { SuperAdmin, "reviews.moderate" }
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
                    { Staff, "reviews.moderate" },
                    { Admin, "reviews.moderate" },
                    { SuperAdmin, "reviews.moderate" }
                });
        }
    }
}
