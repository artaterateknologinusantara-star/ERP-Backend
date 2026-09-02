using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class FilterSoftDeletedFromSalesOrderQuotationIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_QuotationId",
                table: "SalesOrders");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1390), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1390), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1400), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1400), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1400), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1400), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1510), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1510), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_QuotationId",
                table: "SalesOrders",
                column: "QuotationId",
                unique: true,
                filter: "[QuotationId] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_QuotationId",
                table: "SalesOrders");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9380), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9380), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_QuotationId",
                table: "SalesOrders",
                column: "QuotationId",
                unique: true,
                filter: "[QuotationId] IS NOT NULL");
        }
    }
}
