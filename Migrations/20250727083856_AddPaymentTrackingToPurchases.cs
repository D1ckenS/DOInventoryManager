using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DOInventoryManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTrackingToPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaymentAmount",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentAmountUSD",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 27, 11, 38, 55, 607, DateTimeKind.Local).AddTicks(162));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 27, 11, 38, 55, 607, DateTimeKind.Local).AddTicks(173));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 27, 11, 38, 55, 607, DateTimeKind.Local).AddTicks(175));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentAmount",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PaymentAmountUSD",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Purchases");

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
    }
}
