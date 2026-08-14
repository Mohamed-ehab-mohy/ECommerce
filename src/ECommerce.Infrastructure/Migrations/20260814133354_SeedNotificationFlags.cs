using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedNotificationFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var utcNow = DateTime.UtcNow;

            migrationBuilder.InsertData(
                table: "feature_flags",
                columns: ["id", "key", "description", "enabled", "created_at", "updated_at", "is_deleted"],
                values: new object[,]
                {
                    {
                        new Guid("66666666-6666-6666-6666-666666666601"),
                        "notifications.order-confirmation.enabled",
                        "Enables order confirmation emails to customers.",
                        true,
                        utcNow,
                        utcNow,
                        false
                    },
                    {
                        new Guid("66666666-6666-6666-6666-666666666602"),
                        "notifications.order-cancelled.enabled",
                        "Enables order cancellation emails to customers.",
                        true,
                        utcNow,
                        utcNow,
                        false
                    },
                    {
                        new Guid("66666666-6666-6666-6666-666666666603"),
                        "notifications.order-shipped.enabled",
                        "Enables order shipped emails to customers.",
                        true,
                        utcNow,
                        utcNow,
                        false
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "feature_flags",
                keyColumn: "key",
                keyValues: new object[]
                {
                    "notifications.order-confirmation.enabled",
                    "notifications.order-cancelled.enabled",
                    "notifications.order-shipped.enabled"
                });
        }
    }
}
