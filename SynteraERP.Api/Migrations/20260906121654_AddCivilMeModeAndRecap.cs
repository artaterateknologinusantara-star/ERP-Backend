using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCivilMeModeAndRecap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCivilMeMode",
                table: "Quotations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAreaSqm",
                table: "Quotations",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecapUnit",
                table: "QuotationGroups",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecapVolume",
                table: "QuotationGroups",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCivilMeMode",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "TotalAreaSqm",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "RecapUnit",
                table: "QuotationGroups");

            migrationBuilder.DropColumn(
                name: "RecapVolume",
                table: "QuotationGroups");
        }
    }
}
