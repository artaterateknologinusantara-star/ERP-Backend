using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class ItemMasterV2_PricingSeparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("5a8b0257-4808-4a10-88ce-71147552f3d7"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("5e2da09b-e9ef-41a2-80ed-94e51fa82ed5"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("69912368-26e4-49ea-b67d-fcc783f190f4"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("c9270054-8202-4ae9-a8cf-456c2dbf0e76"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("d5c2ae7f-8c36-4819-b0ee-e2988ee29c1d"));

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "ItemMasters",
                newName: "SellingPrice");

            migrationBuilder.AddColumn<bool>(
                name: "IsInventoryItem",
                table: "ItemMasters",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastPurchasePrice",
                table: "ItemMasters",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                table: "ItemMasters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "ItemMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreferredVendorId",
                table: "ItemMasters",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcurementNotes",
                table: "ItemMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "ItemMasters",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReorderPoint",
                table: "ItemMasters",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorItemCode",
                table: "ItemMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalDeleted = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("10f4900d-0d6b-462f-92ea-2d6b6f4ebe19"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6671), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("33b684ad-2bc5-484f-8169-25f7fc8d3fbe"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6659), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6b29b503-7817-4ae8-b1dd-a1e6c0f883c9"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6665), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("741c818e-c941-45b7-9818-649af35dca06"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6629), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7ebbb1d6-e6c1-4e75-9a67-02a25c879e40"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6677), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6146), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6149), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6159), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6161), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6168), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6169), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6555), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 13, 6, 42, 16, 354, DateTimeKind.Unspecified).AddTicks(6557), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_ItemMasters_PreferredVendorId",
                table: "ItemMasters",
                column: "PreferredVendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemMasters_Suppliers_PreferredVendorId",
                table: "ItemMasters",
                column: "PreferredVendorId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemMasters_Suppliers_PreferredVendorId",
                table: "ItemMasters");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_ItemMasters_PreferredVendorId",
                table: "ItemMasters");

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("10f4900d-0d6b-462f-92ea-2d6b6f4ebe19"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("33b684ad-2bc5-484f-8169-25f7fc8d3fbe"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("6b29b503-7817-4ae8-b1dd-a1e6c0f883c9"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("741c818e-c941-45b7-9818-649af35dca06"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("7ebbb1d6-e6c1-4e75-9a67-02a25c879e40"));

            migrationBuilder.DropColumn(
                name: "IsInventoryItem",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "LastPurchasePrice",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "LeadTimeDays",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "PreferredVendorId",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "ProcurementNotes",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "ReorderPoint",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "VendorItemCode",
                table: "ItemMasters");

            migrationBuilder.RenameColumn(
                name: "SellingPrice",
                table: "ItemMasters",
                newName: "Price");

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("5a8b0257-4808-4a10-88ce-71147552f3d7"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4635), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("5e2da09b-e9ef-41a2-80ed-94e51fa82ed5"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4638), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("69912368-26e4-49ea-b67d-fcc783f190f4"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4616), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c9270054-8202-4ae9-a8cf-456c2dbf0e76"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4633), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d5c2ae7f-8c36-4819-b0ee-e2988ee29c1d"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4630), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4360), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4361), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4364), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4365), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4369), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4370), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4581), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 6, 9, 14, 9, 55, 254, DateTimeKind.Unspecified).AddTicks(4582), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
