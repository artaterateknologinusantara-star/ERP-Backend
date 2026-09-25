using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class SplitQuotationWorkDetailServiceMaterialPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill decision (task #44, confirmed 2026-09-26): existing blended UnitPrice moves
            // entirely into MaterialPrice, ServicePrice defaults to 0 — TotalHarga stays identical
            // to the pre-migration value, user re-splits manually if an accurate breakdown matters.
            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "QuotationWorkDetails",
                newName: "MaterialPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "ServicePrice",
                table: "QuotationWorkDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 25, 17, 56, 17, 199, DateTimeKind.Unspecified).AddTicks(710), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 25, 17, 56, 17, 199, DateTimeKind.Unspecified).AddTicks(710), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 25, 17, 56, 17, 199, DateTimeKind.Unspecified).AddTicks(720), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 25, 17, 56, 17, 199, DateTimeKind.Unspecified).AddTicks(720), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 25, 17, 56, 17, 199, DateTimeKind.Unspecified).AddTicks(720), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 25, 17, 56, 17, 199, DateTimeKind.Unspecified).AddTicks(720), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 25, 17, 56, 17, 199, DateTimeKind.Unspecified).AddTicks(780), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 25, 17, 56, 17, 199, DateTimeKind.Unspecified).AddTicks(780), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServicePrice",
                table: "QuotationWorkDetails");

            migrationBuilder.RenameColumn(
                name: "MaterialPrice",
                table: "QuotationWorkDetails",
                newName: "UnitPrice");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 25, 17, 38, 13, 976, DateTimeKind.Unspecified).AddTicks(7850), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 25, 17, 38, 13, 976, DateTimeKind.Unspecified).AddTicks(7850), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 25, 17, 38, 13, 976, DateTimeKind.Unspecified).AddTicks(7860), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 25, 17, 38, 13, 976, DateTimeKind.Unspecified).AddTicks(7860), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 25, 17, 38, 13, 976, DateTimeKind.Unspecified).AddTicks(7860), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 25, 17, 38, 13, 976, DateTimeKind.Unspecified).AddTicks(7860), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 25, 17, 38, 13, 976, DateTimeKind.Unspecified).AddTicks(7920), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 25, 17, 38, 13, 976, DateTimeKind.Unspecified).AddTicks(7920), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
