using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "CustomerPOs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPOs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPOs_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPOs_QuotationId",
                table: "CustomerPOs",
                column: "QuotationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPOs");

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
        }
    }
}
