using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("0ce46c49-59fc-4851-bb0a-b3b6b7dbf47a"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("45da9167-b47c-487f-888e-ab0d068a96ac"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("8e3436c9-9a8d-43f0-a3a3-c2535de821e1"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("cbf3e8e5-25c3-430f-a907-7693f1e0f44f"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("e2e97cec-869a-4ca7-9729-8367ee7ba762"));

            migrationBuilder.CreateTable(
                name: "CompanySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FooterText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SignatureName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SignatureTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CompanySettings",
                columns: new[] { "Id", "Address", "CompanyName", "Email", "FooterText", "LogoPath", "Phone", "SignatureName", "SignatureTitle", "UpdatedAt", "Website" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000001"), "Jl. Raya Teknologi No. 1, Jakarta Selatan 12190", "PT Syntera Teknologi Nusantara", "info@syntera.id", "Penawaran ini berlaku selama 14 hari. Harga belum termasuk biaya pengiriman dan instalasi kecuali disebutkan. Pembayaran 50% di muka, sisa 50% setelah pekerjaan selesai.", null, "+62 21 5555-0100", "Budi Santoso", "Sales Manager", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "www.syntera.id" });

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("4c1adabe-4fc9-49d9-ae91-64dd5652bb25"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4964), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("adbc8fad-2552-472c-8e20-1eb7863bd6c0"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4977), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bca36623-f8ca-4af6-889b-05be8943dc07"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4972), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("be17c69d-89d1-4752-a45f-da228974cd61"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4991), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c0d6effc-bd3f-4c15-b753-cabc2f732084"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4981), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4418), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4419), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4427), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4427), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4433), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4434), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4909), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 13, 45, 12, 799, DateTimeKind.Unspecified).AddTicks(4910), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySettings");

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("4c1adabe-4fc9-49d9-ae91-64dd5652bb25"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("adbc8fad-2552-472c-8e20-1eb7863bd6c0"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("bca36623-f8ca-4af6-889b-05be8943dc07"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("be17c69d-89d1-4752-a45f-da228974cd61"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("c0d6effc-bd3f-4c15-b753-cabc2f732084"));

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0ce46c49-59fc-4851-bb0a-b3b6b7dbf47a"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9386), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("45da9167-b47c-487f-888e-ab0d068a96ac"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9391), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("8e3436c9-9a8d-43f0-a3a3-c2535de821e1"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9393), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("cbf3e8e5-25c3-430f-a907-7693f1e0f44f"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9402), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e2e97cec-869a-4ca7-9729-8367ee7ba762"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9399), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9100), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9102), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9105), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9105), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9119), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9120), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9356), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 11, 38, 25, 20, DateTimeKind.Unspecified).AddTicks(9357), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
