using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPostedByUserIdToJournalEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PostedByUserId",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 14, 53, 24, 80, DateTimeKind.Unspecified).AddTicks(5170), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 14, 53, 24, 80, DateTimeKind.Unspecified).AddTicks(5170), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 14, 53, 24, 80, DateTimeKind.Unspecified).AddTicks(5180), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 14, 53, 24, 80, DateTimeKind.Unspecified).AddTicks(5180), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 14, 53, 24, 80, DateTimeKind.Unspecified).AddTicks(5180), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 14, 53, 24, 80, DateTimeKind.Unspecified).AddTicks(5180), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 14, 53, 24, 80, DateTimeKind.Unspecified).AddTicks(5250), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 14, 53, 24, 80, DateTimeKind.Unspecified).AddTicks(5250), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_PostedByUserId",
                table: "JournalEntries",
                column: "PostedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Users_PostedByUserId",
                table: "JournalEntries",
                column: "PostedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Users_PostedByUserId",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_PostedByUserId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "PostedByUserId",
                table: "JournalEntries");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1390), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1390), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1400), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1400), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1400), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1400), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1510), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 2, 11, 21, 36, 757, DateTimeKind.Unspecified).AddTicks(1510), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
