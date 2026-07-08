using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNomorFakturPajakToInvoiceAndSupplierInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("4efada93-8857-4bce-9814-b9e607b2189c"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("a6a75225-ec1d-4c5c-8537-2bf071fb03c9"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("b6a2f048-20ed-4fe1-894b-f1ebba6a10ce"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("c5408131-d6b2-4044-8917-956cd5ed0401"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("c8dd376a-4fbb-4601-84bd-d9f591ac26b6"));

            migrationBuilder.AddColumn<string>(
                name: "NomorFakturPajak",
                table: "SupplierInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomorFakturPajak",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0ba34c01-92c0-4357-93d7-b04e346cc6d3"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4875), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("427300c4-688f-4541-86d3-1f83974b8c56"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4897), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("578d5841-d6fb-44f4-817b-fb28e9632646"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4899), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("64e8d300-9461-4d49-953e-e3ab5f1a9ea8"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4895), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("daaa9e5c-4413-445f-b312-4d61bd650519"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4892), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4691), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4691), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4694), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4695), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4698), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4698), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4850), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4850), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("0ba34c01-92c0-4357-93d7-b04e346cc6d3"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("427300c4-688f-4541-86d3-1f83974b8c56"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("578d5841-d6fb-44f4-817b-fb28e9632646"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("64e8d300-9461-4d49-953e-e3ab5f1a9ea8"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("daaa9e5c-4413-445f-b312-4d61bd650519"));

            migrationBuilder.DropColumn(
                name: "NomorFakturPajak",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "NomorFakturPajak",
                table: "Invoices");

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("4efada93-8857-4bce-9814-b9e607b2189c"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7510), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("a6a75225-ec1d-4c5c-8537-2bf071fb03c9"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7501), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("b6a2f048-20ed-4fe1-894b-f1ebba6a10ce"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7517), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c5408131-d6b2-4044-8917-956cd5ed0401"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7507), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c8dd376a-4fbb-4601-84bd-d9f591ac26b6"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7515), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7263), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7264), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7311), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7311), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7314), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7314), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7475), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 13, 14, 58, 33, 663, DateTimeKind.Unspecified).AddTicks(7475), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
