using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentAverageCostToItemMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentAverageCost",
                table: "ItemMasters",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Migrasi data satu-kali: starting point CurrentAverageCost = PurchasePrice saat ini
            // (bukan hitung ulang histori), default 0 kalau PurchasePrice belum pernah diisi.
            migrationBuilder.Sql("UPDATE [ItemMasters] SET [CurrentAverageCost] = ISNULL([PurchasePrice], 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentAverageCost",
                table: "ItemMasters");
        }
    }
}
