using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "payments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxProducts = table.Column<int>(type: "integer", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    SupportsCustomDomain = table.Column<bool>(type: "boolean", nullable: false),
                    AdvancedAnalytics = table.Column<bool>(type: "boolean", nullable: false),
                    MaxRequestsPerSecond = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StripeCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_Tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_SubscriptionPlans_tenant_id",
                table: "SubscriptionPlans",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_PlanId",
                table: "TenantSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "ix_TenantSubscriptions_tenant_id",
                table: "TenantSubscriptions",
                column: "tenant_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "version",
                table: "payments");
        }
    }
}
