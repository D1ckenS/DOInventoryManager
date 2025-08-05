using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DOInventoryManager.Migrations
{
    /// <inheritdoc />
    public partial class MakeLegsCompletedNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "LegsCompleted",
                table: "Consumptions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 5, 19, 50, 9, 449, DateTimeKind.Local).AddTicks(1152));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 5, 19, 50, 9, 449, DateTimeKind.Local).AddTicks(1166));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 5, 19, 50, 9, 449, DateTimeKind.Local).AddTicks(1168));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "LegsCompleted",
                table: "Consumptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

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
    }
}
