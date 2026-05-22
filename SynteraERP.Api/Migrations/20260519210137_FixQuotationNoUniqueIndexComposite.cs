using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixQuotationNoUniqueIndexComposite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotations_No",
                table: "Quotations");

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("5d09a198-a001-49fd-91d1-887e965de0a2"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("650173a0-c248-41ab-9b26-2342e47b10f6"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("6515bffc-34c1-42ea-8af2-7a880134bfa6"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("95a2b5dd-49db-4442-92ac-ad568b0a08cd"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("cfa1f76a-e6c3-4499-bb2f-7a6b4b98e6f6"));

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("14d2399a-9953-4b1b-8d88-97c6c4e00285"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(4589), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("3b2a11ef-9ae6-492c-bcfe-de06be5a81df"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(4575), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("5f1c3143-3131-4a10-a3c8-75258246c692"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(4570), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("a2b662b6-2aea-46e8-90bb-8d293cf299ff"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(4582), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e6c6cf06-ee22-45eb-8d83-a1a00053736c"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(4578), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(3957), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(3957), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(3967), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(3968), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(3972), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(3973), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(4525), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 21, 1, 36, 602, DateTimeKind.Unspecified).AddTicks(4526), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_No_Revision",
                table: "Quotations",
                columns: new[] { "No", "Revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotations_No_Revision",
                table: "Quotations");

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("14d2399a-9953-4b1b-8d88-97c6c4e00285"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("3b2a11ef-9ae6-492c-bcfe-de06be5a81df"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("5f1c3143-3131-4a10-a3c8-75258246c692"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("a2b662b6-2aea-46e8-90bb-8d293cf299ff"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("e6c6cf06-ee22-45eb-8d83-a1a00053736c"));

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("5d09a198-a001-49fd-91d1-887e965de0a2"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2447), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("650173a0-c248-41ab-9b26-2342e47b10f6"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2449), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6515bffc-34c1-42ea-8af2-7a880134bfa6"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2439), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("95a2b5dd-49db-4442-92ac-ad568b0a08cd"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2441), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("cfa1f76a-e6c3-4499-bb2f-7a6b4b98e6f6"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2434), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2080), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2081), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2095), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2096), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2099), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2100), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2413), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 19, 50, 41, 607, DateTimeKind.Unspecified).AddTicks(2414), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_No",
                table: "Quotations",
                column: "No",
                unique: true);
        }
    }
}
