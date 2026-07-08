using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNumberingConfigHasData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NumberingConfig DeleteData yang di-scaffold EF Core di sini SENGAJA DIHAPUS MANUAL.
            // Root cause: NumberingConfig dulu di-seed lewat HasData() di OnModelCreating; setelah
            // HasData itu dihapus (dipindah ke NumberingConfigSeeder.cs, idempotent startup seed),
            // EF secara otomatis men-scaffold DeleteData untuk 8 baris yang "hilang" dari model.
            // Menjalankan DeleteData ini akan MENGHAPUS baris NumberingConfig sungguhan di database
            // (bukan cuma reset nilai) — jauh lebih parah dari insiden reset LastNumber sebelumnya.
            // Migration ini SENGAJA dibuat no-op untuk tabel NumberingConfigs: baris-baris itu tetap
            // ada di database apa adanya, tanggung jawab keberadaannya berpindah permanen ke
            // NumberingConfigSeeder (di luar sistem migration). Lihat 03_DEVELOPMENT_ROADMAP.md.

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 13, 34, 28, 916, DateTimeKind.Unspecified).AddTicks(8450), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 13, 34, 28, 916, DateTimeKind.Unspecified).AddTicks(8451), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 13, 34, 28, 916, DateTimeKind.Unspecified).AddTicks(8455), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 13, 34, 28, 916, DateTimeKind.Unspecified).AddTicks(8455), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 13, 34, 28, 916, DateTimeKind.Unspecified).AddTicks(8470), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 13, 34, 28, 916, DateTimeKind.Unspecified).AddTicks(8471), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 13, 34, 28, 916, DateTimeKind.Unspecified).AddTicks(8641), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 13, 34, 28, 916, DateTimeKind.Unspecified).AddTicks(8641), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Pasangan InsertData untuk NumberingConfigs juga SENGAJA DIHAPUS MANUAL - lihat penjelasan
            // lengkap di Up(). Revert migration ini tidak perlu mengembalikan baris NumberingConfig
            // apa pun karena migration ini tidak pernah menghapusnya di Up().

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4691), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4691), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4694), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4695), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4698), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4698), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4850), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 8, 11, 42, 22, 528, DateTimeKind.Unspecified).AddTicks(4850), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
