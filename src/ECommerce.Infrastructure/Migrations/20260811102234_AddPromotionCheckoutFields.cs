using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionCheckoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "applied_promotion_ids",
                table: "orders",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<Guid>(
                name: "coupon_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "applied_coupon_id",
                table: "checkouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "applied_promotion_ids",
                table: "checkouts",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "applied_coupon_code",
                table: "carts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "applied_promotion_ids",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "coupon_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "applied_coupon_id",
                table: "checkouts");

            migrationBuilder.DropColumn(
                name: "applied_promotion_ids",
                table: "checkouts");

            migrationBuilder.DropColumn(
                name: "applied_coupon_code",
                table: "carts");
        }
    }
}
