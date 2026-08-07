using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Data;

public static class SupplierSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;

        var suppliers = new List<Supplier>
        {
            new() { Id = new Guid("60000000-0000-0000-0000-000000000001"), Code = "SUPP0002", Name = "PT Mitra Jaringan Nusantara", ContactPerson = "Bpk. Doni Setiawan", Phone = "021-5789-2201", Email = "sales@mitrajaringan.co.id", Address = "Jl. Mangga Dua Raya No. 45, Blok C2", City = "Jakarta Utara", Npwp = "02.111.222.3-051.000", BankName = "Bank Mandiri", BankAccount = "1230098765432", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("60000000-0000-0000-0000-000000000002"), Code = "SUPP0003", Name = "PT Cakrawala Vision Teknologi", ContactPerson = "Ibu Melly Anggraini", Phone = "021-2957-3312", Email = "marketing@cakrawalavision.com", Address = "Jl. Kebon Jeruk Raya No. 18", City = "Jakarta Barat", Npwp = "02.222.333.4-052.000", BankName = "Bank BCA", BankAccount = "4560112233", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("60000000-0000-0000-0000-000000000003"), Code = "SUPP0004", Name = "PT Nusantara Fiber Optindo", ContactPerson = "Bpk. Yudha Pratama", Phone = "022-7301-4455", Email = "procurement@fiberoptindo.co.id", Address = "Jl. Soekarno Hatta No. 210", City = "Bandung", Npwp = "02.333.444.5-053.000", BankName = "Bank BNI", BankAccount = "0221334455", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("60000000-0000-0000-0000-000000000004"), Code = "SUPP0005", Name = "PT Datacom Infra Perkasa", ContactPerson = "Ibu Ratna Kusuma", Phone = "021-8990-6671", Email = "sales@datacominfra.com", Address = "Jl. TB Simatupang Kav. 88", City = "Jakarta Selatan", Npwp = "02.444.555.6-051.001", BankName = "Bank Mandiri", BankAccount = "1230011223344", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("60000000-0000-0000-0000-000000000005"), Code = "SUPP0006", Name = "PT Powertama Energi Solusi", ContactPerson = "Bpk. Fajar Nugraha", Phone = "021-4585-9902", Email = "info@powertamaenergi.co.id", Address = "Jl. Raya Bekasi Km. 21", City = "Bekasi", Npwp = "02.555.666.7-054.000", BankName = "Bank BRI", BankAccount = "0089112233", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("60000000-0000-0000-0000-000000000006"), Code = "SUPP0007", Name = "UD Sumber Rejeki Perkantoran", ContactPerson = "Ibu Wati Handayani", Phone = "021-6390-1187", Email = "order@sumberrejekikantor.com", Address = "Jl. Kelapa Hybrida Raya No. 7", City = "Jakarta Utara", Npwp = "02.666.777.8-051.002", BankName = "Bank BCA", BankAccount = "4569988776", IsActive = true, CreatedAt = now, UpdatedAt = now },
        };

        // Per-item idempotent check (not a single table-wide AnyAsync guard like CustomerSeeder) —
        // this table may already hold suppliers created manually through the app (e.g. while testing
        // the Vendor form) before this seeder ever ran, and those must not block the dummy batch below.
        foreach (var s in suppliers)
        {
            if (await db.Suppliers.AnyAsync(x => x.Code == s.Code)) continue;
            db.Suppliers.Add(s);
        }

        await db.SaveChangesAsync();
    }
}
