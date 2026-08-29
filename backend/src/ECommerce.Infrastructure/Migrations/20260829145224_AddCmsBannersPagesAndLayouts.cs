using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCmsBannersPagesAndLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banners",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    image_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    target_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banners", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cms_layouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cms_layouts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    html_content = table.Column<string>(type: "text", nullable: false),
                    meta_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cms_layout_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    config_json = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cms_layout_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_cms_layout_sections_cms_layouts_layout_id",
                        column: x => x.layout_id,
                        principalTable: "cms_layouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_banners_tenant_id",
                table: "banners",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_cms_layout_sections_layout_id",
                table: "cms_layout_sections",
                column: "layout_id");

            migrationBuilder.CreateIndex(
                name: "ix_cms_layouts_slug",
                table: "cms_layouts",
                column: "slug");

            migrationBuilder.CreateIndex(
                name: "ix_cms_layouts_tenant_id",
                table: "cms_layouts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_pages_slug",
                table: "pages",
                column: "slug");

            migrationBuilder.CreateIndex(
                name: "ix_pages_tenant_id",
                table: "pages",
                column: "tenant_id");

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: ["role_id", "permission_code"],
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.banner.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.banner.write" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.banner.delete" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.page.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.page.write" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.page.delete" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.layout.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.layout.write" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.layout.delete" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.banner.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.banner.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.banner.delete" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.page.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.page.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.page.delete" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.layout.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.layout.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.layout.delete" }
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
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.banner.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.banner.write" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.banner.delete" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.page.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.page.write" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.page.delete" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.layout.read" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.layout.write" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "content.layout.delete" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.banner.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.banner.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.banner.delete" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.page.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.page.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.page.delete" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.layout.read" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.layout.write" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "content.layout.delete" }
                });

            migrationBuilder.DropTable(
                name: "banners");

            migrationBuilder.DropTable(
                name: "cms_layout_sections");

            migrationBuilder.DropTable(
                name: "pages");

            migrationBuilder.DropTable(
                name: "cms_layouts");
        }
    }
}
