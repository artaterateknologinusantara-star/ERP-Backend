using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Regresi untuk insiden "Server Blade 2U": dulu GetShippableItemsForSoAsync (dipakai layar Buat
// DO) fallback ke tebak "kata pertama nama item" saat SO line-nya tidak match SKU apa pun — dan
// FirstOrDefaultAsync tanpa OrderBy bisa memilih Item Master lain yang cuma kebetulan mirip nama
// ("Server Rack 42U" line ke-match ke Item Master "Server Blade 2U" yang stoknya 0 dan tidak
// terkait sama sekali). Fallback tebak-nama itu SUDAH DIHAPUS TOTAL — suite ini mengunci bahwa
// baris ambigu/tanpa SKU match tidak PERNAH auto-match, harus di-link manual via
// LinkSoItemToItemMasterAsync.
public class DeliveryOrderItemMasterMatchingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public DeliveryOrderItemMasterMatchingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private IServiceProvider CreateScratchServices()
    {
        var connectionString = "Server=localhost,1433;Database=SynteraERP_Scratch;User Id=sa;Password=DevgvImMkAaOBHs4CP5kWRsLLyM!9q;TrustServerCertificate=True;Encrypt=False;";
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-signing-key-not-used-anywhere-else-32chars");

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));
            });
        });

        return factory.Services;
    }

    private static ItemMaster NewItemMaster(string code, string name, decimal stock) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = name,
        Uom = "Unit",
        Stock = stock,
        MinStock = 0,
        SellingPrice = 1_000_000,
        IsActive = true,
    };

    private static SalesOrder NewOpenSalesOrder(string no) => new()
    {
        Id = Guid.NewGuid(),
        No = no,
        CustomerId = SeededCustomerId,
        ProjectName = "Test Project " + no,
        Date = DateOnly.FromDateTime(DateTime.UtcNow),
        SalesId = SeededAdminId,
        Status = SalesOrderStatus.Open,
        Total = 1_000_000,
    };

    [Fact]
    public async Task Ambiguous_name_with_no_matching_Sku_is_reported_Unmatched_not_guessed()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        // Two Item Masters that share the same first word ("Server...") — this ambiguity is
        // exactly what made the old name-fallback pick the wrong one.
        var rack = NewItemMaster($"RACK-{suffix}", $"Server Rack 42U {suffix}", stock: 5);
        var blade = NewItemMaster($"BLADE-{suffix}", $"Server Blade 2U {suffix}", stock: 0);
        db.ItemMasters.AddRange(rack, blade);

        var so = NewOpenSalesOrder("TST-SO-AMBIG-" + suffix);
        so.Items.Add(new SalesOrderItem
        {
            Id = Guid.NewGuid(),
            Description = $"Server Rack 42U {suffix}",
            Sku = "2.1", // Quotation-style line number, NOT a real ItemMaster.Code — must not match.
            Qty = 1,
            Uom = "Unit",
            UnitPrice = 1_000_000,
            Amount = 1_000_000,
            SortOrder = 0,
        });
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync();

        var inventorySvc = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var result = await inventorySvc.GetShippableItemsForSoAsync(so.Id);

        result.Matched.Should().BeEmpty("no Sku/ItemMasterId matched explicitly — must NOT guess from the name");
        result.Unmatched.Should().ContainSingle()
            .Which.Description.Should().Be($"Server Rack 42U {suffix}");

        await CleanupAsync(db, so.Id, rack.Id, blade.Id);
    }

    [Fact]
    public async Task After_manual_link_the_SO_line_becomes_Matched_using_the_correct_ItemMaster()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var rack = NewItemMaster($"RACK-{suffix}", $"Server Rack 42U {suffix}", stock: 5);
        var blade = NewItemMaster($"BLADE-{suffix}", $"Server Blade 2U {suffix}", stock: 0);
        db.ItemMasters.AddRange(rack, blade);

        var so = NewOpenSalesOrder("TST-SO-LINK-" + suffix);
        var soItem = new SalesOrderItem
        {
            Id = Guid.NewGuid(),
            Description = $"Server Rack 42U {suffix}",
            Sku = "2.1",
            Qty = 1,
            Uom = "Unit",
            UnitPrice = 1_000_000,
            Amount = 1_000_000,
            SortOrder = 0,
        };
        so.Items.Add(soItem);
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync();

        var inventorySvc = scope.ServiceProvider.GetRequiredService<IInventoryService>();

        // Simulate the user picking the CORRECT item manually in the "Buat DO" screen.
        await inventorySvc.LinkSoItemToItemMasterAsync(soItem.Id, rack.Id);

        var result = await inventorySvc.GetShippableItemsForSoAsync(so.Id);

        result.Unmatched.Should().BeEmpty();
        var matched = result.Matched.Should().ContainSingle().Subject;
        matched.ItemMasterId.Should().Be(rack.Id, "must resolve to the item the user explicitly linked, not the ambiguous 'Blade' one");
        matched.StockAvailable.Should().Be(5);

        await CleanupAsync(db, so.Id, rack.Id, blade.Id);
    }

    [Fact]
    public async Task CreateDOFromSOAsync_skips_unmatched_lines_instead_of_guessing_by_name()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var switchItem = NewItemMaster($"SW-{suffix}", $"Managed Switch 24 Port {suffix}", stock: 10);
        var blade = NewItemMaster($"BLADE-{suffix}", $"Server Blade 2U {suffix}", stock: 0);
        db.ItemMasters.AddRange(switchItem, blade);

        var so = NewOpenSalesOrder("TST-SO-FROMSO-" + suffix);
        so.Items.Add(new SalesOrderItem
        {
            Id = Guid.NewGuid(),
            Description = $"Managed Switch 24 Port {suffix}",
            Sku = switchItem.Code, // exact Code match — should resolve fine.
            Qty = 2,
            Uom = "Unit",
            UnitPrice = 500_000,
            Amount = 1_000_000,
            SortOrder = 0,
        });
        so.Items.Add(new SalesOrderItem
        {
            Id = Guid.NewGuid(),
            Description = $"Server Rack 42U {suffix}", // no matching ItemMaster/Sku at all in this test.
            Sku = "2.1",
            Qty = 1,
            Uom = "Unit",
            UnitPrice = 1_000_000,
            Amount = 1_000_000,
            SortOrder = 1,
        });
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync();

        var inventorySvc = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var doDto = await inventorySvc.CreateDOFromSOAsync(so.Id, SeededAdminId);

        doDto.Items.Should().ContainSingle("only the exact-Sku-matched line should make it into the DO")
            .Which.ItemMasterId.Should().Be(switchItem.Id);

        await CleanupAsync(db, so.Id, switchItem.Id, blade.Id, doDto.Id);
    }

    private static async Task CleanupAsync(AppDbContext db, Guid soId, Guid itemMasterId1, Guid itemMasterId2, Guid? doId = null)
    {
        if (doId.HasValue)
        {
            var deliveryOrder = await db.DeliveryOrders.FirstOrDefaultAsync(x => x.Id == doId.Value);
            if (deliveryOrder is not null) db.DeliveryOrders.Remove(deliveryOrder);
        }

        var so = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == soId);
        if (so is not null) db.SalesOrders.Remove(so);

        var items = await db.ItemMasters.Where(x => x.Id == itemMasterId1 || x.Id == itemMasterId2).ToListAsync();
        db.ItemMasters.RemoveRange(items);

        await db.SaveChangesAsync();
    }
}
