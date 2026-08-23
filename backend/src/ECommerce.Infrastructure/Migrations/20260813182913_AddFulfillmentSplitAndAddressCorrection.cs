using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFulfillmentSplitAndAddressCorrection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_fulfillment_tasks_order_id",
                table: "fulfillment_tasks");

            migrationBuilder.AddColumn<Guid>(
                name: "parent_task_id",
                table: "fulfillment_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_fulfillment_tasks_order_id",
                table: "fulfillment_tasks",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_fulfillment_tasks_parent_task_id",
                table: "fulfillment_tasks",
                column: "parent_task_id");

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_tasks_parent",
                table: "fulfillment_tasks",
                column: "parent_task_id",
                principalTable: "fulfillment_tasks",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_tasks_parent",
                table: "fulfillment_tasks");

            migrationBuilder.DropIndex(
                name: "ix_fulfillment_tasks_order_id",
                table: "fulfillment_tasks");

            migrationBuilder.DropIndex(
                name: "ix_fulfillment_tasks_parent_task_id",
                table: "fulfillment_tasks");

            migrationBuilder.DropColumn(
                name: "parent_task_id",
                table: "fulfillment_tasks");

            migrationBuilder.CreateIndex(
                name: "ux_fulfillment_tasks_order_id",
                table: "fulfillment_tasks",
                column: "order_id",
                unique: true);
        }
    }
}
