using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Data;

/// <summary>
/// Seed NumberingConfig lewat idempotent check saat startup (pola sama seperti CustomerSeeder/
/// ItemMasterSeeder), BUKAN lewat HasData() di AppDbContext.OnModelCreating. HasData() diproses ulang
/// oleh EF Core setiap kali migration baru dibuat, dan pernah menyebabkan LastNumber ter-reset ke nilai
/// seed lama saat Id-nya non-deterministic (insiden PPN Reconciliation, 2026-07-08) — lihat
/// 03_DEVELOPMENT_ROADMAP.md bagian Technical Debt untuk detail lengkap. Nilai awal di bawah HANYA
/// dipakai kalau baris untuk DocType itu belum ada sama sekali (instalasi baru) — tidak pernah
/// menimpa baris yang sudah ada.
///
/// Suffix perusahaan (di belakang titik, mis. "SYN" pada "Q.SYN") dibaca dari
/// CompanySettings.DocumentPrefix HANYA untuk baris yang baru dibuat di sini — mengubah
/// DocumentPrefix di Company Settings TIDAK PERNAH mengubah baris NumberingConfig yang sudah ada
/// (prinsip sama seperti di atas: jangan pernah menimpa histori penomoran yang sudah berjalan).
/// </summary>
public static class NumberingConfigSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var companyPrefix = await db.CompanySettings.Select(c => c.DocumentPrefix).FirstOrDefaultAsync();
        var suffix = string.IsNullOrWhiteSpace(companyPrefix) ? "SYN" : companyPrefix.Trim();

        await SeedOneAsync(db, "QUOTATION", $"Q.{suffix}");
        await SeedOneAsync(db, "SALES_ORDER", $"SO.{suffix}");
        await SeedOneAsync(db, "INVOICE", $"INV.{suffix}");
        await SeedOneAsync(db, "PURCHASE_REQUEST", $"PR.{suffix}");
        await SeedOneAsync(db, "PURCHASE_ORDER", $"PO.{suffix}");
        await SeedOneAsync(db, "JOURNAL_ENTRY", $"JE.{suffix}");
        await SeedOneAsync(db, "SUPPLIER_INVOICE", $"SINV.{suffix}");
        await SeedOneAsync(db, "EXPENSE", $"EXP.{suffix}");
        await SeedOneAsync(db, "DELIVERY_ORDER", $"DO.{suffix}");
    }

    private static async Task SeedOneAsync(AppDbContext db, string docType, string prefix)
    {
        if (await db.NumberingConfigs.AnyAsync(n => n.DocType == docType)) return;

        db.NumberingConfigs.Add(new NumberingConfig
        {
            DocType = docType,
            Prefix = prefix,
            LastNumber = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
