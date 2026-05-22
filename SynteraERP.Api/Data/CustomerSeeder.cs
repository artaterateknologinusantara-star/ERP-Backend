using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Data;

public static class CustomerSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Ensure admin password is correct (re-hash on startup if needed)
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@syntera.id");
        if (admin is not null && !BCrypt.Net.BCrypt.Verify("Admin@123", admin.PasswordHash))
        {
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            await db.SaveChangesAsync();
        }

        if (await db.Customers.AnyAsync()) return;

        var now = DateTimeOffset.UtcNow;

        var customers = new List<Customer>
        {
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000001"), Code = "CUST-001", Name = "PT Telkom Indonesia (Persero) Tbk", Industry = "Telekomunikasi", ContactPerson = "Bpk. Rendra Prasetyo", Phone = "021-5474000", Email = "procurement@telkom.co.id", Address = "Jl. Japati No. 1, Bandung 40133", City = "Bandung", Npwp = "01.000.000.1-093.000", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000002"), Code = "CUST-002", Name = "PT Bank Central Asia Tbk", Industry = "Perbankan", ContactPerson = "Ibu Sari Dewi Kusuma", Phone = "021-23588000", Email = "it.procurement@bca.co.id", Address = "Jl. MH Thamrin No. 1, Jakarta Pusat 10310", City = "Jakarta Pusat", Npwp = "01.001.000.1-051.000", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000003"), Code = "CUST-003", Name = "PT Pertamina (Persero)", Industry = "Energi & Migas", ContactPerson = "Bpk. Eko Santoso", Phone = "021-3815111", Email = "it.infra@pertamina.com", Address = "Jl. Medan Merdeka Timur No. 1A, Jakarta Pusat 10110", City = "Jakarta Pusat", Npwp = "01.000.133.1-051.000", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000004"), Code = "CUST-004", Name = "PT Astra International Tbk", Industry = "Otomotif & Manufaktur", ContactPerson = "Ibu Linda Wulandari", Phone = "021-6522555", Email = "procurement@astra.co.id", Address = "Jl. Gaya Motor Raya No. 8, Jakarta Utara 14330", City = "Jakarta Utara", Npwp = "01.001.985.1-054.000", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000005"), Code = "CUST-005", Name = "PT Bank Mandiri (Persero) Tbk", Industry = "Perbankan", ContactPerson = "Bpk. Ahmad Fauzi", Phone = "021-5299-7777", Email = "it.purchase@bankmandiri.co.id", Address = "Jl. Jend. Gatot Subroto Kav. 36-38, Jakarta Selatan 12190", City = "Jakarta Selatan", Npwp = "01.000.000.1-051.001", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000006"), Code = "CUST-006", Name = "PT Indosat Ooredoo Hutchison Tbk", Industry = "Telekomunikasi", ContactPerson = "Bpk. Hendra Wijaya", Phone = "021-30001000", Email = "vendor@indosat.com", Address = "Jl. Medan Merdeka Barat No. 21, Jakarta Pusat 10110", City = "Jakarta Pusat", Npwp = "01.000.000.1-051.002", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000007"), Code = "CUST-007", Name = "PT PLN (Persero)", Industry = "Utilitas & Energi", ContactPerson = "Ibu Rina Marlina", Phone = "021-7261122", Email = "pengadaan@pln.co.id", Address = "Jl. Trunojoyo Blok M-I No. 135, Jakarta Selatan 12160", City = "Jakarta Selatan", Npwp = "01.000.000.1-051.003", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000008"), Code = "CUST-008", Name = "PT XL Axiata Tbk", Industry = "Telekomunikasi", ContactPerson = "Bpk. Dimas Ardiansyah", Phone = "021-57959000", Email = "procurement@xl.co.id", Address = "Jl. HR Rasuna Said X5 Kav. 11-12, Jakarta Selatan 12950", City = "Jakarta Selatan", Npwp = "01.001.000.1-051.004", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000009"), Code = "CUST-009", Name = "PT Unilever Indonesia Tbk", Industry = "FMCG", ContactPerson = "Ibu Dewi Anggraeni", Phone = "021-7884-9100", Email = "vendor.id@unilever.com", Address = "Jl. BSD Boulevard Barat, BSD City, Tangerang Selatan 15322", City = "Tangerang Selatan", Npwp = "01.001.000.1-054.001", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000010"), Code = "CUST-010", Name = "PT Bukit Asam Tbk", Industry = "Pertambangan", ContactPerson = "Bpk. Yusuf Halim", Phone = "0734-452452", Email = "it@bukitasam.co.id", Address = "Jl. Parigi No. 1 Tanjung Enim 31716, Sumatera Selatan", City = "Tanjung Enim", Npwp = "01.002.000.1-054.002", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000011"), Code = "CUST-011", Name = "PT Kimia Farma Tbk", Industry = "Farmasi & Kesehatan", ContactPerson = "Ibu Nita Rahayu", Phone = "021-4520223", Email = "procurement@kimiafarma.co.id", Address = "Jl. Veteran No. 9, Jakarta Pusat 10110", City = "Jakarta Pusat", Npwp = "01.000.000.1-051.005", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000012"), Code = "CUST-012", Name = "PT Semen Indonesia (Persero) Tbk", Industry = "Manufaktur", ContactPerson = "Bpk. Rizal Fathoni", Phone = "031-3981732", Email = "pengadaan@semenindonesia.com", Address = "Jl. Veteran No. 66, Gresik, Jawa Timur 61122", City = "Gresik", Npwp = "01.000.000.1-095.000", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000013"), Code = "CUST-013", Name = "PT Garuda Indonesia (Persero) Tbk", Industry = "Penerbangan", ContactPerson = "Bpk. Satrio Nugroho", Phone = "021-23519999", Email = "it.infra@garuda-indonesia.com", Address = "Jl. Kebon Sirih No. 44, Jakarta Pusat 10110", City = "Jakarta Pusat", Npwp = "01.000.000.1-051.006", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000014"), Code = "CUST-014", Name = "PT Krakatau Steel (Persero) Tbk", Industry = "Industri Baja", ContactPerson = "Ibu Sri Wahyuni", Phone = "0254-371000", Email = "procurement@krakatausteel.com", Address = "Jl. Industri No. 5, Cilegon, Banten 42435", City = "Cilegon", Npwp = "01.000.000.1-054.003", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = new Guid("C0000000-0000-0000-0000-000000000015"), Code = "CUST-015", Name = "PT Wijaya Karya (Persero) Tbk", Industry = "Konstruksi & Infrastruktur", ContactPerson = "Bpk. Agus Hermawan", Phone = "021-4260051", Email = "it@wika.co.id", Address = "Jl. DI Panjaitan Kav. 9, Jakarta Timur 13340", City = "Jakarta Timur", Npwp = "01.000.000.1-051.007", IsActive = true, CreatedAt = now, UpdatedAt = now },
        };

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();
    }
}
