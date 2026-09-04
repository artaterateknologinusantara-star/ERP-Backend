using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCashBankAccountIdToPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashBankAccountId",
                table: "SalesOrderPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CashBankAccountId",
                table: "POPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CashBankAccountId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderPayments_CashBankAccountId",
                table: "SalesOrderPayments",
                column: "CashBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_POPayments_CashBankAccountId",
                table: "POPayments",
                column: "CashBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CashBankAccountId",
                table: "Payments",
                column: "CashBankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Accounts_CashBankAccountId",
                table: "Payments",
                column: "CashBankAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_POPayments_Accounts_CashBankAccountId",
                table: "POPayments",
                column: "CashBankAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderPayments_Accounts_CashBankAccountId",
                table: "SalesOrderPayments",
                column: "CashBankAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Backfill POPayments existing (AP) ke akun Kas (1-1001) - sebelum migration ini semua
            // Cash Out PO memang selalu diposting ke 1-1001, jadi ini murni membuat data historis
            // konsisten dengan apa yang sudah terjadi di GL, bukan mengubah histori. Payments (AR)
            // sengaja TIDAK dibackfill: kosong di database development saat migration ini ditulis,
            // dan kalaupun ada baris di lingkungan lain, membiarkannya NULL lebih jujur (kolom ini
            // baru ada mulai migration ini) daripada menebak akun yang tidak pernah tercatat.
            migrationBuilder.Sql(@"
                UPDATE POPayments
                SET CashBankAccountId = (SELECT TOP 1 Id FROM Accounts WHERE Code = '1-1001' AND IsDeleted = 0)
                WHERE CashBankAccountId IS NULL
                  AND EXISTS (SELECT 1 FROM Accounts WHERE Code = '1-1001' AND IsDeleted = 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Accounts_CashBankAccountId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_POPayments_Accounts_CashBankAccountId",
                table: "POPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderPayments_Accounts_CashBankAccountId",
                table: "SalesOrderPayments");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderPayments_CashBankAccountId",
                table: "SalesOrderPayments");

            migrationBuilder.DropIndex(
                name: "IX_POPayments_CashBankAccountId",
                table: "POPayments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CashBankAccountId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CashBankAccountId",
                table: "SalesOrderPayments");

            migrationBuilder.DropColumn(
                name: "CashBankAccountId",
                table: "POPayments");

            migrationBuilder.DropColumn(
                name: "CashBankAccountId",
                table: "Payments");
        }
    }
}
