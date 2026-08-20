using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdAndQueryFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "wishlists",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "webhook_endpoints",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "webhook_deliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stock_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "shipments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "review_votes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "return_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "refunds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "promotions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_variants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_imports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "payment_reconciliation_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "notification_preferences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "mfa_secrets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "idempotency_keys",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "fulfillment_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "feature_flags",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "export_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "customer_addresses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "credit_notes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "coupons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "checkouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "checkout_saga_states",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "carts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "brands",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_wishlists_tenant_id",
                table: "wishlists",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoints_tenant_id",
                table: "webhook_endpoints",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_deliveries_tenant_id",
                table: "webhook_deliveries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_tenant_id",
                table: "warehouses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_tenant_id",
                table: "stock_movements",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_tenant_id",
                table: "stock_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_tenant_id",
                table: "shipments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_tenant_id",
                table: "roles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_votes_tenant_id",
                table: "review_votes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_tenant_id",
                table: "return_requests",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_refunds_tenant_id",
                table: "refunds",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_tenant_id",
                table: "refresh_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotions_tenant_id",
                table: "promotions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id",
                table: "products",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_tenant_id",
                table: "product_variants",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_tenant_id",
                table: "product_reviews",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_imports_tenant_id",
                table: "product_imports",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_id",
                table: "payments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_records_tenant_id",
                table: "payment_reconciliation_records",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id",
                table: "orders",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_tenant_id",
                table: "notification_preferences",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_mfa_secrets_tenant_id",
                table: "mfa_secrets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_id",
                table: "invoices",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_tenant_id",
                table: "idempotency_keys",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_fulfillment_tasks_tenant_id",
                table: "fulfillment_tasks",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_feature_flags_tenant_id",
                table: "feature_flags",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_tenant_id",
                table: "export_jobs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_tenant_id",
                table: "customers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_addresses_tenant_id",
                table: "customer_addresses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_notes_tenant_id",
                table: "credit_notes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupons_tenant_id",
                table: "coupons",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_checkouts_tenant_id",
                table: "checkouts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_checkout_saga_states_tenant_id",
                table: "checkout_saga_states",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_tenant_id",
                table: "categories",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_carts_tenant_id",
                table: "carts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_brands_tenant_id",
                table: "brands",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_wishlists_tenant_id",
                table: "wishlists");

            migrationBuilder.DropIndex(
                name: "ix_webhook_endpoints_tenant_id",
                table: "webhook_endpoints");

            migrationBuilder.DropIndex(
                name: "ix_webhook_deliveries_tenant_id",
                table: "webhook_deliveries");

            migrationBuilder.DropIndex(
                name: "ix_warehouses_tenant_id",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_tenant_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_items_tenant_id",
                table: "stock_items");

            migrationBuilder.DropIndex(
                name: "ix_shipments_tenant_id",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "ix_roles_tenant_id",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_review_votes_tenant_id",
                table: "review_votes");

            migrationBuilder.DropIndex(
                name: "ix_return_requests_tenant_id",
                table: "return_requests");

            migrationBuilder.DropIndex(
                name: "ix_refunds_tenant_id",
                table: "refunds");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_tenant_id",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_promotions_tenant_id",
                table: "promotions");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_product_variants_tenant_id",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "ix_product_reviews_tenant_id",
                table: "product_reviews");

            migrationBuilder.DropIndex(
                name: "ix_product_imports_tenant_id",
                table: "product_imports");

            migrationBuilder.DropIndex(
                name: "ix_payments_tenant_id",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "ix_payment_reconciliation_records_tenant_id",
                table: "payment_reconciliation_records");

            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_notification_preferences_tenant_id",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "ix_mfa_secrets_tenant_id",
                table: "mfa_secrets");

            migrationBuilder.DropIndex(
                name: "ix_invoices_tenant_id",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_idempotency_keys_tenant_id",
                table: "idempotency_keys");

            migrationBuilder.DropIndex(
                name: "ix_fulfillment_tasks_tenant_id",
                table: "fulfillment_tasks");

            migrationBuilder.DropIndex(
                name: "ix_feature_flags_tenant_id",
                table: "feature_flags");

            migrationBuilder.DropIndex(
                name: "ix_export_jobs_tenant_id",
                table: "export_jobs");

            migrationBuilder.DropIndex(
                name: "ix_customers_tenant_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customer_addresses_tenant_id",
                table: "customer_addresses");

            migrationBuilder.DropIndex(
                name: "ix_credit_notes_tenant_id",
                table: "credit_notes");

            migrationBuilder.DropIndex(
                name: "ix_coupons_tenant_id",
                table: "coupons");

            migrationBuilder.DropIndex(
                name: "ix_checkouts_tenant_id",
                table: "checkouts");

            migrationBuilder.DropIndex(
                name: "ix_checkout_saga_states_tenant_id",
                table: "checkout_saga_states");

            migrationBuilder.DropIndex(
                name: "ix_categories_tenant_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_carts_tenant_id",
                table: "carts");

            migrationBuilder.DropIndex(
                name: "ix_brands_tenant_id",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "wishlists");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "webhook_deliveries");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "review_votes");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "return_requests");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "promotions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_reviews");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_imports");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "payment_reconciliation_records");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "mfa_secrets");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "idempotency_keys");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "fulfillment_tasks");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "feature_flags");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "export_jobs");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "customer_addresses");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "credit_notes");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "checkouts");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "checkout_saga_states");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "carts");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "brands");
        }
    }
}
