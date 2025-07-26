using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DOInventoryManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseBalanceAfterToAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseBalanceAfter",
                table: "Allocations",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 26, 17, 19, 50, 486, DateTimeKind.Local).AddTicks(8166));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 26, 17, 19, 50, 486, DateTimeKind.Local).AddTicks(8179));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 26, 17, 19, 50, 486, DateTimeKind.Local).AddTicks(8180));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseBalanceAfter",
                table: "Allocations");

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 26, 3, 24, 53, 556, DateTimeKind.Local).AddTicks(1889));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 26, 3, 24, 53, 556, DateTimeKind.Local).AddTicks(1900));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 26, 3, 24, 53, 556, DateTimeKind.Local).AddTicks(1902));
        }
    }
}
