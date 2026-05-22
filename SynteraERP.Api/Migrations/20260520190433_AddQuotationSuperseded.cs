using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationSuperseded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByQuotationId",
                table: "Quotations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("24de1bf5-7fa0-4db2-98e5-e83bf2d0fbcd"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4840), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("3a564d8a-3d2b-4f72-a472-aae77e582801"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4857), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("4d0ec25b-1877-4f22-8b8d-179b138c1f3c"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4834), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("62df74d1-e4d4-44e4-9b88-f225cf8f589b"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4849), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e63af99a-5d8e-485d-8d0a-70360ba892a3"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4853), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4327), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4327), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4334), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4335), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4339), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4340), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4772), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 20, 19, 4, 32, 353, DateTimeKind.Unspecified).AddTicks(4773), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_SupersededByQuotationId",
                table: "Quotations",
                column: "SupersededByQuotationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_Quotations_SupersededByQuotationId",
                table: "Quotations",
                column: "SupersededByQuotationId",
                principalTable: "Quotations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_Quotations_SupersededByQuotationId",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_SupersededByQuotationId",
                table: "Quotations");

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("24de1bf5-7fa0-4db2-98e5-e83bf2d0fbcd"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("3a564d8a-3d2b-4f72-a472-aae77e582801"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("4d0ec25b-1877-4f22-8b8d-179b138c1f3c"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("62df74d1-e4d4-44e4-9b88-f225cf8f589b"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("e63af99a-5d8e-485d-8d0a-70360ba892a3"));

            migrationBuilder.DropColumn(
                name: "SupersededByQuotationId",
                table: "Quotations");

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
    }
}
