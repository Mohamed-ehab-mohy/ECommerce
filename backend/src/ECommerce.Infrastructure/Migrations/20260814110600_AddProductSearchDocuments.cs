using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSearchDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "product_search_documents",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    brand = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    list_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    rating_average = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    rating_count = table.Column<int>(type: "integer", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "setweight(to_tsvector('simple', coalesce(name, '')), 'A') ||\nsetweight(to_tsvector('simple', coalesce(description, '')), 'B') ||\nsetweight(to_tsvector('simple', coalesce(brand, '')), 'C') ||\nsetweight(to_tsvector('simple', coalesce(sku, '')), 'D')", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_search_documents", x => new { x.product_id, x.locale });
                    table.ForeignKey(
                        name: "fk_product_search_documents_product",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_locale_brand",
                table: "product_search_documents",
                columns: new[] { "locale", "brand_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_locale_category",
                table: "product_search_documents",
                columns: new[] { "locale", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_locale_price",
                table: "product_search_documents",
                columns: new[] { "locale", "list_amount" });

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_name_trgm",
                table: "product_search_documents",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_search_vector",
                table: "product_search_documents",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.Sql(
                """
                INSERT INTO product_search_documents (
                    product_id, locale, name, description, sku, brand, brand_id,
                    category, category_id, list_amount, currency, rating_average, rating_count)
                SELECT
                    p.id,
                    t.locale,
                    t.name,
                    t.description,
                    p.sku,
                    b.name,
                    b.id,
                    c.name,
                    c.id,
                    pp.list_amount,
                    pp.currency,
                    0,
                    0
                FROM products p
                INNER JOIN product_translations t ON t.product_id = p.id
                LEFT JOIN brands b ON b.id = p.brand_id
                LEFT JOIN categories c ON c.id = p.category_id
                INNER JOIN (
                    SELECT product_id, currency, list_amount,
                           ROW_NUMBER() OVER (PARTITION BY product_id ORDER BY currency) AS rn
                    FROM product_prices
                ) pp ON pp.product_id = p.id AND pp.rn = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_search_documents");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
