using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddItemMasterEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("0e7571ea-e82c-4d9f-9b33-a4c7859ecab4"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("19ad6189-7c9e-4f08-8cb1-f00b67b11f33"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("7e518652-ee87-427f-96f4-a93e07054e35"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("81ff545b-2a1a-4036-bf7f-bb1a2b67d4ff"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("d1d9f212-154c-4b94-a9ff-a07d7d387269"));

            migrationBuilder.CreateTable(
                name: "ItemMasters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Uom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Warehouse = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Stock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MinStock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemMasters", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("1a434a5c-15d9-4984-a74f-a8dfb94b1970"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1823), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("2ca38d06-794d-44f6-9717-10173a7429fb"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1839), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("8d465c86-515c-42ac-86d2-0de7b2af3fd9"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1831), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("a048ece7-c35d-41d0-9b25-8f8377b18271"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1834), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ceb7f556-d9e6-4a43-a276-ee46c4cf465b"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1828), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1543), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1544), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1548), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1548), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1551), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1552), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1786), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1787), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_ItemMasters_Code",
                table: "ItemMasters",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemMasters");

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("1a434a5c-15d9-4984-a74f-a8dfb94b1970"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("2ca38d06-794d-44f6-9717-10173a7429fb"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("8d465c86-515c-42ac-86d2-0de7b2af3fd9"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("a048ece7-c35d-41d0-9b25-8f8377b18271"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("ceb7f556-d9e6-4a43-a276-ee46c4cf465b"));

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0e7571ea-e82c-4d9f-9b33-a4c7859ecab4"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1175), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("19ad6189-7c9e-4f08-8cb1-f00b67b11f33"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1173), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7e518652-ee87-427f-96f4-a93e07054e35"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1184), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("81ff545b-2a1a-4036-bf7f-bb1a2b67d4ff"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1182), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d1d9f212-154c-4b94-a9ff-a07d7d387269"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1169), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(853), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(854), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(858), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(858), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(871), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(872), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1140), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1141), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
