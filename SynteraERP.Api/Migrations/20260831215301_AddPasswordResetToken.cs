using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 31, 21, 53, 1, 595, DateTimeKind.Unspecified).AddTicks(2230), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 31, 21, 53, 1, 595, DateTimeKind.Unspecified).AddTicks(2240), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 31, 21, 53, 1, 595, DateTimeKind.Unspecified).AddTicks(2240), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 31, 21, 53, 1, 595, DateTimeKind.Unspecified).AddTicks(2240), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 31, 21, 53, 1, 595, DateTimeKind.Unspecified).AddTicks(2240), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 31, 21, 53, 1, 595, DateTimeKind.Unspecified).AddTicks(2240), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 31, 21, 53, 1, 595, DateTimeKind.Unspecified).AddTicks(2320), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 31, 21, 53, 1, 595, DateTimeKind.Unspecified).AddTicks(2320), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 31, 21, 33, 45, 349, DateTimeKind.Unspecified).AddTicks(3150), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 31, 21, 33, 45, 349, DateTimeKind.Unspecified).AddTicks(3150), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 31, 21, 33, 45, 349, DateTimeKind.Unspecified).AddTicks(3150), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 31, 21, 33, 45, 349, DateTimeKind.Unspecified).AddTicks(3150), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 31, 21, 33, 45, 349, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 31, 21, 33, 45, 349, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 31, 21, 33, 45, 349, DateTimeKind.Unspecified).AddTicks(3240), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 31, 21, 33, 45, 349, DateTimeKind.Unspecified).AddTicks(3240), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
