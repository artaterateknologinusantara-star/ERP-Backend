using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class GenericizeCompanySettingsSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded, not a plain UpdateData: only touches the seed row if it still holds the
            // exact old hardcoded defaults. A row already customized via PUT /api/company-settings
            // (e.g. renamed to a real client's brand) no longer matches this WHERE clause and is
            // left untouched — see ERP-Backend/CLAUDE.md rule #9 for why a blind UpdateData here
            // would be dangerous (it would silently overwrite live-customized data on every
            // environment that hasn't run this migration yet).
            migrationBuilder.Sql(@"
UPDATE dbo.CompanySettings
SET CompanyName = N'Perusahaan Anda',
    Email = NULL,
    SignatureName = NULL,
    SignatureTitle = NULL,
    UpdatedAt = SYSDATETIMEOFFSET()
WHERE Id = '30000000-0000-0000-0000-000000000001'
  AND CompanyName = N'PT Syntera Teknologi Nusantara'
  AND Email = N'info@syntera.id'
  AND SignatureName = N'Budi Santoso'
  AND SignatureTitle = N'Sales Manager';
");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 23, 11, 28, 9, 69, DateTimeKind.Unspecified).AddTicks(1500), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 23, 11, 28, 9, 69, DateTimeKind.Unspecified).AddTicks(1500), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 23, 11, 28, 9, 69, DateTimeKind.Unspecified).AddTicks(1500), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 23, 11, 28, 9, 69, DateTimeKind.Unspecified).AddTicks(1500), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 23, 11, 28, 9, 69, DateTimeKind.Unspecified).AddTicks(1500), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 23, 11, 28, 9, 69, DateTimeKind.Unspecified).AddTicks(1510), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 23, 11, 28, 9, 69, DateTimeKind.Unspecified).AddTicks(1570), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 23, 11, 28, 9, 69, DateTimeKind.Unspecified).AddTicks(1570), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mirror guard: only reverts rows that still hold the exact generic placeholder this
            // migration wrote in Up() — a row customized after this migration ran is left alone.
            migrationBuilder.Sql(@"
UPDATE dbo.CompanySettings
SET CompanyName = N'PT Syntera Teknologi Nusantara',
    Email = N'info@syntera.id',
    SignatureName = N'Budi Santoso',
    SignatureTitle = N'Sales Manager',
    UpdatedAt = SYSDATETIMEOFFSET()
WHERE Id = '30000000-0000-0000-0000-000000000001'
  AND CompanyName = N'Perusahaan Anda'
  AND Email IS NULL
  AND SignatureName IS NULL
  AND SignatureTitle IS NULL;
");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 23, 8, 53, 33, 508, DateTimeKind.Unspecified).AddTicks(7870), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 23, 8, 53, 33, 508, DateTimeKind.Unspecified).AddTicks(7870), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 23, 8, 53, 33, 508, DateTimeKind.Unspecified).AddTicks(7870), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 23, 8, 53, 33, 508, DateTimeKind.Unspecified).AddTicks(7870), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 23, 8, 53, 33, 508, DateTimeKind.Unspecified).AddTicks(7870), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 23, 8, 53, 33, 508, DateTimeKind.Unspecified).AddTicks(7870), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 23, 8, 53, 33, 508, DateTimeKind.Unspecified).AddTicks(7940), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 23, 8, 53, 33, 508, DateTimeKind.Unspecified).AddTicks(7940), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
