using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class FilterSoftDeletedFromCodeUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Code",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ItemMasters_Code",
                table: "ItemMasters");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Code",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Branches_Code",
                table: "Branches");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9320), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9380), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 47, 34, 102, DateTimeKind.Unspecified).AddTicks(9380), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ItemMasters_Code",
                table: "ItemMasters",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Code",
                table: "Customers",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Code",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ItemMasters_Code",
                table: "ItemMasters");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Code",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Branches_Code",
                table: "Branches");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 37, 10, 905, DateTimeKind.Unspecified).AddTicks(3760), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 37, 10, 905, DateTimeKind.Unspecified).AddTicks(3760), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 37, 10, 905, DateTimeKind.Unspecified).AddTicks(3770), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 37, 10, 905, DateTimeKind.Unspecified).AddTicks(3770), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 37, 10, 905, DateTimeKind.Unspecified).AddTicks(3770), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 37, 10, 905, DateTimeKind.Unspecified).AddTicks(3770), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 9, 37, 10, 905, DateTimeKind.Unspecified).AddTicks(3840), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 9, 37, 10, 905, DateTimeKind.Unspecified).AddTicks(3840), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemMasters_Code",
                table: "ItemMasters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Code",
                table: "Customers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code",
                unique: true);
        }
    }
}
