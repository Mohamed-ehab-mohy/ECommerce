using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_code });
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_customers_user_id",
                        column: x => x.user_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            var seededAt = new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "roles",
                columns: ["id", "name", "description", "created_at", "updated_at", "is_deleted"],
                values: new object[,]
                {
                    { SeedRoleIds.Customer, "Customer", "Store customer with self-service access.", seededAt, seededAt, false },
                    { SeedRoleIds.Staff, "Staff", "Support staff with customer lookup and audit access.", seededAt, seededAt, false },
                    { SeedRoleIds.Finance, "Finance", "Finance users with audit visibility.", seededAt, seededAt, false },
                    { SeedRoleIds.Admin, "Admin", "Administrator with catalog and role management.", seededAt, seededAt, false },
                    { SeedRoleIds.SuperAdmin, "SuperAdmin", "Super administrator with all permissions.", seededAt, seededAt, false }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: ["role_id", "permission_code"],
                values: new object[,]
                {
                    { SeedRoleIds.Staff, "customers.read" },
                    { SeedRoleIds.Staff, "audit.read" },
                    { SeedRoleIds.Finance, "audit.read" },
                    { SeedRoleIds.Admin, "catalog.product.write" },
                    { SeedRoleIds.Admin, "catalog.product.delete" },
                    { SeedRoleIds.Admin, "roles.read" },
                    { SeedRoleIds.Admin, "roles.write" },
                    { SeedRoleIds.Admin, "roles.permissions.write" },
                    { SeedRoleIds.Admin, "customers.read" },
                    { SeedRoleIds.Admin, "audit.read" },
                    { SeedRoleIds.SuperAdmin, "catalog.product.write" },
                    { SeedRoleIds.SuperAdmin, "catalog.product.delete" },
                    { SeedRoleIds.SuperAdmin, "roles.read" },
                    { SeedRoleIds.SuperAdmin, "roles.write" },
                    { SeedRoleIds.SuperAdmin, "roles.permissions.write" },
                    { SeedRoleIds.SuperAdmin, "customers.read" },
                    { SeedRoleIds.SuperAdmin, "audit.read" },
                    { SeedRoleIds.SuperAdmin, "auth.impersonate" }
                });
        }

        private static class SeedRoleIds
        {
            public static readonly Guid Customer = new("11111111-1111-1111-1111-111111111111");
            public static readonly Guid Staff = new("22222222-2222-2222-2222-222222222222");
            public static readonly Guid Finance = new("33333333-3333-3333-3333-333333333333");
            public static readonly Guid Admin = new("44444444-4444-4444-4444-444444444444");
            public static readonly Guid SuperAdmin = new("55555555-5555-5555-5555-555555555555");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
