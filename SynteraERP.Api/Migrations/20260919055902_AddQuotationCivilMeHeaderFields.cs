using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationCivilMeHeaderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AreaBlockTender",
                table: "Quotations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Contractor",
                table: "Quotations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacilityId",
                table: "Quotations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacilityName",
                table: "Quotations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Quotations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RenovPic",
                table: "Quotations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeOfWork",
                table: "Quotations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidityPeriod",
                table: "Quotations",
                type: "nvarchar(max)",
                nullable: true);

            // NOTE: EF Core auto-scaffolded UpdateData calls here that would reset CreatedAt/UpdatedAt
            // on the 3 seeded Roles + 1 seeded admin User to "now" (DateTimeOffset.UtcNow baked as a
            // fresh literal into HasData() every time the model is diffed) — same failure class as the
            // NumberingConfig HasData incident (see 03_DEVELOPMENT_ROADMAP.md). Deliberately stripped;
            // this migration only touches Quotations columns. Pre-existing in prior migrations too —
            // out of scope for this change, flagged separately.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaBlockTender",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "Contractor",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "FacilityName",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "RenovPic",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "ScopeOfWork",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "ValidityPeriod",
                table: "Quotations");

            // Roles/Users UpdateData stripped here too — see matching note in Up() above.
        }
    }
}
