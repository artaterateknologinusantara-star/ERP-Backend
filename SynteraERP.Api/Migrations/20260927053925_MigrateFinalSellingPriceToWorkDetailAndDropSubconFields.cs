using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class MigrateFinalSellingPriceToWorkDetailAndDropSubconFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate any pre-existing QuotationGroup.FinalSellingPrice into a real
            // QuotationWorkDetail row BEFORE the column is dropped below, so GrandTotal for that
            // Quotation does not change by even one Rupiah. RecalcTotals summed FinalSellingPrice
            // + Item + WorkDetail before this migration; after it, only Item + WorkDetail — so the
            // value must already exist as a real WorkDetail row by the time the column disappears.
            // Confirmed against real dev-DB data before writing this (investigation task #8): 2
            // Draft Quotations found with non-zero FinalSellingPrice (Rp 3.000.000 and
            // Rp 5.000.000), neither converted to a SalesOrder yet.
            //
            // One new QuotationWorkItem + one QuotationWorkDetail per affected Group:
            // - WorkItem.SortOrder = 1 past whatever WorkItem already exists in that Group (0 if
            //   none) — computed once into a table variable before any insert, so it reads only
            //   pre-existing WorkItems.
            // - WorkDetail.ServicePrice = FinalSellingPrice (Harga Jual — the same number that was
            //   flowing into TotalService via RecalcTotals; the WorkDetail row now carries that
            //   role instead). MaterialPrice = 0, Volume = 1, Unit = 'Ls'.
            // - FinalSubconCost (cost-basis, was NEVER summed into any total) has no column of its
            //   own in the new schema — if it was set, its value is preserved as a free-text audit
            //   note in WorkDetail.Spesifikasi instead of silently disappearing from the record.
            //
            // Runs exactly once: EF Core's own __EFMigrationsHistory bookkeeping prevents this
            // migration's Up() from ever being re-applied against a database that already has it
            // recorded, so there is no risk of this INSERT running twice and duplicating rows.
            migrationBuilder.Sql(@"
                DECLARE @Affected TABLE (GroupId UNIQUEIDENTIFIER, WorkItemId UNIQUEIDENTIFIER, NextSortOrder INT);

                INSERT INTO @Affected (GroupId, WorkItemId, NextSortOrder)
                SELECT qg.Id, NEWID(),
                       ISNULL((SELECT MAX(wi.SortOrder) + 1 FROM QuotationWorkItems wi WHERE wi.GroupId = qg.Id), 0)
                FROM QuotationGroups qg
                WHERE qg.FinalSellingPrice IS NOT NULL AND qg.FinalSellingPrice <> 0;

                INSERT INTO QuotationWorkItems (Id, GroupId, Name, SortOrder, SourceVendorRabRequestId)
                SELECT a.WorkItemId, a.GroupId, N'Migrasi Harga Jual Subkontraktor (data lama)', a.NextSortOrder, NULL
                FROM @Affected a;

                INSERT INTO QuotationWorkDetails (Id, WorkItemId, Name, Spesifikasi, Volume, Unit, ServicePrice, MaterialPrice, SortOrder)
                SELECT NEWID(), a.WorkItemId, N'Harga Jual Subkontraktor (migrasi dari data manual lama)',
                       CASE WHEN qg.FinalSubconCost IS NOT NULL
                            THEN N'Catatan migrasi: biaya subcon lama (cost-basis, tidak pernah dihitung ke total) = Rp ' + CONVERT(NVARCHAR(50), qg.FinalSubconCost)
                            ELSE NULL END,
                       1, N'Ls', qg.FinalSellingPrice, 0, 0
                FROM @Affected a
                JOIN QuotationGroups qg ON qg.Id = a.GroupId;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_QuotationGroups_Suppliers_SubcontractorId",
                table: "QuotationGroups");

            migrationBuilder.DropIndex(
                name: "IX_QuotationGroups_SubcontractorId",
                table: "QuotationGroups");

            migrationBuilder.DropColumn(
                name: "FinalSellingPrice",
                table: "QuotationGroups");

            migrationBuilder.DropColumn(
                name: "FinalSubconCost",
                table: "QuotationGroups");

            migrationBuilder.DropColumn(
                name: "SubcontractorId",
                table: "QuotationGroups");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 5, 39, 25, 592, DateTimeKind.Unspecified).AddTicks(8060), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 5, 39, 25, 592, DateTimeKind.Unspecified).AddTicks(8060), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 5, 39, 25, 592, DateTimeKind.Unspecified).AddTicks(8070), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 5, 39, 25, 592, DateTimeKind.Unspecified).AddTicks(8070), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 5, 39, 25, 592, DateTimeKind.Unspecified).AddTicks(8070), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 5, 39, 25, 592, DateTimeKind.Unspecified).AddTicks(8070), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 9, 27, 5, 39, 25, 592, DateTimeKind.Unspecified).AddTicks(8130), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 27, 5, 39, 25, 592, DateTimeKind.Unspecified).AddTicks(8140), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately one-way/lossy: this only restores the dropped COLUMNS (as empty/NULL),
            // it does NOT reconstruct their values from the QuotationWorkItem/WorkDetail rows that
            // Up() created, and it does NOT delete those migrated rows either. Rolling back after
            // Up() has run leaves the migrated WorkDetail rows in place (still correctly counted
            // in GrandTotal via the 2-source formula) while the old FinalSellingPrice/
            // FinalSubconCost/SubcontractorId columns come back empty. This is intentional, not an
            // oversight — reconstructing "which WorkDetail row came from which old column value"
            // after the fact is not reliably possible once other edits may have touched the group.
            migrationBuilder.AddColumn<decimal>(
                name: "FinalSellingPrice",
                table: "QuotationGroups",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalSubconCost",
                table: "QuotationGroups",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubcontractorId",
                table: "QuotationGroups",
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
                name: "IX_QuotationGroups_SubcontractorId",
                table: "QuotationGroups",
                column: "SubcontractorId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationGroups_Suppliers_SubcontractorId",
                table: "QuotationGroups",
                column: "SubcontractorId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
