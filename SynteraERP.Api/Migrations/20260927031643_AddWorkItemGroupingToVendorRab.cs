using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemGroupingToVendorRab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorRabRequests_QuotationWorkItems_ApprovedWorkItemId",
                table: "VendorRabRequests");

            migrationBuilder.DropIndex(
                name: "IX_VendorRabRequests_ApprovedWorkItemId",
                table: "VendorRabRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedWorkItemId",
                table: "VendorRabRequests");

            migrationBuilder.AddColumn<string>(
                name: "WorkItemName",
                table: "VendorRabSubmissionLines",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceVendorRabRequestId",
                table: "QuotationWorkItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 3, 16, 43, 393, DateTimeKind.Unspecified).AddTicks(480), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 3, 16, 43, 393, DateTimeKind.Unspecified).AddTicks(480), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 3, 16, 43, 393, DateTimeKind.Unspecified).AddTicks(480), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 3, 16, 43, 393, DateTimeKind.Unspecified).AddTicks(480), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 3, 16, 43, 393, DateTimeKind.Unspecified).AddTicks(490), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 3, 16, 43, 393, DateTimeKind.Unspecified).AddTicks(490), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 3, 16, 43, 393, DateTimeKind.Unspecified).AddTicks(560), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 3, 16, 43, 393, DateTimeKind.Unspecified).AddTicks(560), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_QuotationWorkItems_SourceVendorRabRequestId",
                table: "QuotationWorkItems",
                column: "SourceVendorRabRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationWorkItems_VendorRabRequests_SourceVendorRabRequestId",
                table: "QuotationWorkItems",
                column: "SourceVendorRabRequestId",
                principalTable: "VendorRabRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuotationWorkItems_VendorRabRequests_SourceVendorRabRequestId",
                table: "QuotationWorkItems");

            migrationBuilder.DropIndex(
                name: "IX_QuotationWorkItems_SourceVendorRabRequestId",
                table: "QuotationWorkItems");

            migrationBuilder.DropColumn(
                name: "WorkItemName",
                table: "VendorRabSubmissionLines");

            migrationBuilder.DropColumn(
                name: "SourceVendorRabRequestId",
                table: "QuotationWorkItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedWorkItemId",
                table: "VendorRabRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 2, 24, 39, 628, DateTimeKind.Unspecified).AddTicks(7360), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 2, 24, 39, 628, DateTimeKind.Unspecified).AddTicks(7360), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 2, 24, 39, 628, DateTimeKind.Unspecified).AddTicks(7360), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 2, 24, 39, 628, DateTimeKind.Unspecified).AddTicks(7360), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 2, 24, 39, 628, DateTimeKind.Unspecified).AddTicks(7370), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 2, 24, 39, 628, DateTimeKind.Unspecified).AddTicks(7370), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 2, 24, 39, 628, DateTimeKind.Unspecified).AddTicks(7430), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 2, 24, 39, 628, DateTimeKind.Unspecified).AddTicks(7430), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_VendorRabRequests_ApprovedWorkItemId",
                table: "VendorRabRequests",
                column: "ApprovedWorkItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorRabRequests_QuotationWorkItems_ApprovedWorkItemId",
                table: "VendorRabRequests",
                column: "ApprovedWorkItemId",
                principalTable: "QuotationWorkItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
