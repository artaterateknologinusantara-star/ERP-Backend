using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddItemMasterIdToPRAndPOItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<Guid>(
                name: "ItemMasterId",
                table: "PurchaseRequestItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemMasterId",
                table: "PurchaseOrderItems",
                type: "uniqueidentifier",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestItems_ItemMasterId",
                table: "PurchaseRequestItems",
                column: "ItemMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ItemMasterId",
                table: "PurchaseOrderItems",
                column: "ItemMasterId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_ItemMasters_ItemMasterId",
                table: "PurchaseOrderItems",
                column: "ItemMasterId",
                principalTable: "ItemMasters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequestItems_ItemMasters_ItemMasterId",
                table: "PurchaseRequestItems",
                column: "ItemMasterId",
                principalTable: "ItemMasters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_ItemMasters_ItemMasterId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequestItems_ItemMasters_ItemMasterId",
                table: "PurchaseRequestItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequestItems_ItemMasterId",
                table: "PurchaseRequestItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderItems_ItemMasterId",
                table: "PurchaseOrderItems");

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

            migrationBuilder.DropColumn(
                name: "ItemMasterId",
                table: "PurchaseRequestItems");

            migrationBuilder.DropColumn(
                name: "ItemMasterId",
                table: "PurchaseOrderItems");

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
        }
    }
}
