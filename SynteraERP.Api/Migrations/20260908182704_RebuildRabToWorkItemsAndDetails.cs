using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class RebuildRabToWorkItemsAndDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RabAttachmentPath",
                table: "QuotationGroups");

            migrationBuilder.CreateTable(
                name: "QuotationWorkItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationWorkItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationWorkItems_QuotationGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "QuotationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotationWorkDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Spesifikasi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationWorkDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationWorkDetails_QuotationWorkItems_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "QuotationWorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotationWorkDetailAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationWorkDetailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationWorkDetailAttachments_QuotationWorkDetails_WorkDetailId",
                        column: x => x.WorkDetailId,
                        principalTable: "QuotationWorkDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuotationWorkDetailAttachments_WorkDetailId",
                table: "QuotationWorkDetailAttachments",
                column: "WorkDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationWorkDetails_WorkItemId",
                table: "QuotationWorkDetails",
                column: "WorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationWorkItems_GroupId",
                table: "QuotationWorkItems",
                column: "GroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuotationWorkDetailAttachments");

            migrationBuilder.DropTable(
                name: "QuotationWorkDetails");

            migrationBuilder.DropTable(
                name: "QuotationWorkItems");

            migrationBuilder.AddColumn<string>(
                name: "RabAttachmentPath",
                table: "QuotationGroups",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
