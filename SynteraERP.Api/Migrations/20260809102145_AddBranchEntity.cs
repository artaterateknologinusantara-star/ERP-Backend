using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Manager = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 9, 10, 21, 45, 455, DateTimeKind.Unspecified).AddTicks(5290), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 9, 10, 21, 45, 455, DateTimeKind.Unspecified).AddTicks(5290), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 9, 10, 21, 45, 455, DateTimeKind.Unspecified).AddTicks(5290), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 9, 10, 21, 45, 455, DateTimeKind.Unspecified).AddTicks(5290), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 9, 10, 21, 45, 455, DateTimeKind.Unspecified).AddTicks(5300), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 9, 10, 21, 45, 455, DateTimeKind.Unspecified).AddTicks(5300), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 9, 10, 21, 45, 455, DateTimeKind.Unspecified).AddTicks(5360), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 9, 10, 21, 45, 455, DateTimeKind.Unspecified).AddTicks(5360), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 7, 18, 13, 50, 667, DateTimeKind.Unspecified).AddTicks(1470), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 7, 18, 13, 50, 667, DateTimeKind.Unspecified).AddTicks(1470), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 7, 18, 13, 50, 667, DateTimeKind.Unspecified).AddTicks(1470), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 7, 18, 13, 50, 667, DateTimeKind.Unspecified).AddTicks(1470), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 7, 18, 13, 50, 667, DateTimeKind.Unspecified).AddTicks(1470), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 7, 18, 13, 50, 667, DateTimeKind.Unspecified).AddTicks(1470), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 7, 18, 13, 50, 667, DateTimeKind.Unspecified).AddTicks(1550), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 7, 18, 13, 50, 667, DateTimeKind.Unspecified).AddTicks(1550), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
