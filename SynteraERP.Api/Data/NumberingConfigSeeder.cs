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
/// </summary>
public static class NumberingConfigSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedOneAsync(db, "QUOTATION", "Q.SYN");
        await SeedOneAsync(db, "SALES_ORDER", "SO.SYN");
        await SeedOneAsync(db, "INVOICE", "INV.SYN");
        await SeedOneAsync(db, "PURCHASE_REQUEST", "PR.SYN");
        await SeedOneAsync(db, "PURCHASE_ORDER", "PO.SYN");
        await SeedOneAsync(db, "JOURNAL_ENTRY", "JE.SYN");
        await SeedOneAsync(db, "SUPPLIER_INVOICE", "SINV.SYN");
        await SeedOneAsync(db, "EXPENSE", "EXP.SYN");
        await SeedOneAsync(db, "DELIVERY_ORDER", "DO.SYN");
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
