using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerApiTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "partner_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    rate_limit_per_minute = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "partner_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    scopes = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_api_keys", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_partner_accounts_is_active",
                table: "partner_accounts",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_partner_accounts_tenant_id",
                table: "partner_accounts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_partner_accounts_email",
                table: "partner_accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_partner_api_keys_is_active",
                table: "partner_api_keys",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_partner_api_keys_partner_id",
                table: "partner_api_keys",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_partner_api_keys_tenant_id",
                table: "partner_api_keys",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_partner_api_keys_key_hash",
                table: "partner_api_keys",
                column: "key_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partner_accounts");

            migrationBuilder.DropTable(
                name: "partner_api_keys");
        }
    }
}
