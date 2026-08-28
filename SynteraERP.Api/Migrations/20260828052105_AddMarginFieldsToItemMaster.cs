using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMarginFieldsToItemMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSellingPriceManual",
                table: "ItemMasters",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MarginDefault",
                table: "ItemMasters",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MarginMinimum",
                table: "ItemMasters",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarginType",
                table: "ItemMasters",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 28, 5, 21, 4, 746, DateTimeKind.Unspecified).AddTicks(30), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 28, 5, 21, 4, 746, DateTimeKind.Unspecified).AddTicks(30), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 28, 5, 21, 4, 746, DateTimeKind.Unspecified).AddTicks(30), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 28, 5, 21, 4, 746, DateTimeKind.Unspecified).AddTicks(30), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 28, 5, 21, 4, 746, DateTimeKind.Unspecified).AddTicks(30), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 28, 5, 21, 4, 746, DateTimeKind.Unspecified).AddTicks(30), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 28, 5, 21, 4, 746, DateTimeKind.Unspecified).AddTicks(110), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 28, 5, 21, 4, 746, DateTimeKind.Unspecified).AddTicks(110), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSellingPriceManual",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "MarginDefault",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "MarginMinimum",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "MarginType",
                table: "ItemMasters");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 27, 18, 40, 10, 660, DateTimeKind.Unspecified).AddTicks(530), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 27, 18, 40, 10, 660, DateTimeKind.Unspecified).AddTicks(530), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 27, 18, 40, 10, 660, DateTimeKind.Unspecified).AddTicks(540), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 27, 18, 40, 10, 660, DateTimeKind.Unspecified).AddTicks(540), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 27, 18, 40, 10, 660, DateTimeKind.Unspecified).AddTicks(540), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 27, 18, 40, 10, 660, DateTimeKind.Unspecified).AddTicks(540), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 27, 18, 40, 10, 660, DateTimeKind.Unspecified).AddTicks(610), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 27, 18, 40, 10, 660, DateTimeKind.Unspecified).AddTicks(610), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
