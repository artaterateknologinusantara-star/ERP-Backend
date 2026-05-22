using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationSendAndRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("0b664bd4-839d-4ee6-b3b5-221ef7396425"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("98d93e34-2eda-469c-9578-39fa9cd11164"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("c6796f69-70f3-4bed-a132-9605aac10362"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("eabe1004-4edb-4a8a-b052-72c3260c0c8e"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("f584f8a4-2e5c-4c5d-89c3-5e8a9bcf234c"));

            migrationBuilder.AddColumn<bool>(
                name: "IsLatestRevision",
                table: "Quotations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SentAt",
                table: "Quotations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SentBy",
                table: "Quotations",
                type: "uniqueidentifier",
                nullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropColumn(
                name: "IsLatestRevision",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "SentBy",
                table: "Quotations");

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0b664bd4-839d-4ee6-b3b5-221ef7396425"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(230), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("98d93e34-2eda-469c-9578-39fa9cd11164"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(211), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c6796f69-70f3-4bed-a132-9605aac10362"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(232), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("eabe1004-4edb-4a8a-b052-72c3260c0c8e"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(227), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f584f8a4-2e5c-4c5d-89c3-5e8a9bcf234c"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(234), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9956), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9957), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9960), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9961), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9964), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9965), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(186), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(186), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
