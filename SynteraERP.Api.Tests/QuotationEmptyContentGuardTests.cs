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
using SynteraERP.Api.DTOs.Quotation;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Task #42: SendAsync/ApproveAsync must reject a Quotation with no content at all — existence-
// based ("does at least one row exist"), not price-based (GrandTotal<=0 was rejected in Step-0
// because Discount=100% legitimately zeroes GrandTotal on a Quotation that has real rows).
public class QuotationEmptyContentGuardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public QuotationEmptyContentGuardTests(WebApplicationFactory<Program> factory)
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

    private static SaveQuotationRequest BaseRequest(bool isCivilMe, params SaveQuotationGroupRequest[] groups) => new()
    {
        CustomerId = SeededCustomerId,
        SalesId = SeededAdminId,
        ProjectName = "Test Empty Guard " + Guid.NewGuid().ToString("N")[..6],
        Date = DateOnly.FromDateTime(DateTime.UtcNow),
        ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
        Discount = 0,
        TaxRate = 11,
        IsCivilMeMode = isCivilMe,
        Tabs =
        [
            new SaveQuotationTabRequest
            {
                Label = "Tab 1", SortOrder = 0,
                Groups = [.. groups],
            },
        ],
    };

    [Fact]
    public async Task SendAsync_rejects_standard_mode_Quotation_with_zero_Items()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var quotation = await quotationSvc.CreateAsync(BaseRequest(false,
            new SaveQuotationGroupRequest { Name = "Group 1", SortOrder = 0, Items = [] }));

        try
        {
            var act = () => quotationSvc.SendAsync(quotation.Id, SeededAdminId);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*belum ada baris*");
        }
        finally
        {
            var toDelete = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotation.Id);
            if (toDelete is not null) db.Quotations.Remove(toDelete);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task SendAsync_rejects_CivilMe_Quotation_with_zero_Items_zero_WorkDetail_zero_FinalSellingPrice()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var quotation = await quotationSvc.CreateAsync(BaseRequest(true,
            new SaveQuotationGroupRequest { Name = "Group 1", SortOrder = 0, Items = [], FinalSellingPrice = null }));

        try
        {
            var act = () => quotationSvc.SendAsync(quotation.Id, SeededAdminId);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*belum ada baris*");
        }
        finally
        {
            var toDelete = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotation.Id);
            if (toDelete is not null) db.Quotations.Remove(toDelete);
            await db.SaveChangesAsync();
        }
    }

    // Step-0's central finding: GrandTotal<=0 is NOT a valid "kosong" proxy — a Quotation with
    // real rows and Discount=100% legitimately has GrandTotal=0. This must NOT be rejected.
    [Fact]
    public async Task SendAsync_allows_Quotation_with_real_content_even_when_Discount_100_percent_zeroes_GrandTotal()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var request = BaseRequest(false,
            new SaveQuotationGroupRequest
            {
                Name = "Group 1", SortOrder = 0,
                Items =
                [
                    new SaveQuotationItemRequest
                    {
                        ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit",
                        MaterialPrice = 1_000_000, ServicePrice = 0, SortOrder = 0,
                    },
                ],
            });
        request.Discount = 100;
        var quotation = await quotationSvc.CreateAsync(request);

        try
        {
            quotation.GrandTotal.Should().Be(0); // confirms the false-positive scenario is real
            quotation.Discount.Should().Be(100);

            var result = await quotationSvc.SendAsync(quotation.Id, SeededAdminId);
            result.Should().NotBeNull(); // must NOT throw
        }
        finally
        {
            var toDelete = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotation.Id);
            if (toDelete is not null) db.Quotations.Remove(toDelete);
            await db.SaveChangesAsync();
        }
    }

    // Existence-check, not price-check: a single row priced at 0 still counts as content.
    [Fact]
    public async Task SendAsync_allows_Quotation_with_a_single_zero_priced_line()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var quotation = await quotationSvc.CreateAsync(BaseRequest(false,
            new SaveQuotationGroupRequest
            {
                Name = "Group 1", SortOrder = 0,
                Items =
                [
                    new SaveQuotationItemRequest
                    {
                        ItemNo = "1", Equipment = "Free Sample", Qty = 1, Unit = "unit",
                        MaterialPrice = 0, ServicePrice = 0, SortOrder = 0,
                    },
                ],
            }));

        try
        {
            quotation.GrandTotal.Should().Be(0);

            var result = await quotationSvc.SendAsync(quotation.Id, SeededAdminId);
            result.Should().NotBeNull();
        }
        finally
        {
            var toDelete = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotation.Id);
            if (toDelete is not null) db.Quotations.Remove(toDelete);
            await db.SaveChangesAsync();
        }
    }

    // ApproveAsync needs its own independent guard: content can be emptied out between Send and
    // Approve (UpdateAsync doesn't gate on Status), so a Quotation that was valid when sent could
    // still reach Approve empty if only SendAsync checked.
    [Fact]
    public async Task ApproveAsync_rejects_Quotation_emptied_out_after_it_was_sent()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var quotation = await quotationSvc.CreateAsync(BaseRequest(false,
            new SaveQuotationGroupRequest
            {
                Name = "Group 1", SortOrder = 0,
                Items =
                [
                    new SaveQuotationItemRequest
                    {
                        ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit",
                        MaterialPrice = 1_000_000, ServicePrice = 0, SortOrder = 0,
                    },
                ],
            }));

        try
        {
            await quotationSvc.SendAsync(quotation.Id, SeededAdminId); // succeeds — has content

            // Emptied out while still Terkirim (UpdateAsync doesn't gate on Status).
            var emptyRequest = BaseRequest(false,
                new SaveQuotationGroupRequest
                {
                    Id = quotation.Tabs[0].Groups[0].Id,
                    Name = "Group 1", SortOrder = 0, Items = [],
                });
            emptyRequest.Tabs[0].Id = quotation.Tabs[0].Id;
            await quotationSvc.UpdateAsync(quotation.Id, emptyRequest);

            var act = () => quotationSvc.ApproveAsync(quotation.Id, SeededAdminId);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*belum ada baris*");
        }
        finally
        {
            var toDelete = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotation.Id);
            if (toDelete is not null) db.Quotations.Remove(toDelete);
            await db.SaveChangesAsync();
        }
    }

    [Theory]
    [InlineData(150, 100)]
    [InlineData(-10, 0)]
    [InlineData(50, 50)]
    public async Task Discount_is_clamped_to_0_100_range_on_create(decimal input, decimal expected)
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var request = BaseRequest(false,
            new SaveQuotationGroupRequest
            {
                Name = "Group 1", SortOrder = 0,
                Items =
                [
                    new SaveQuotationItemRequest
                    {
                        ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit",
                        MaterialPrice = 1_000_000, ServicePrice = 0, SortOrder = 0,
                    },
                ],
            });
        request.Discount = input;
        var quotation = await quotationSvc.CreateAsync(request);

        try
        {
            quotation.Discount.Should().Be(expected);
        }
        finally
        {
            var toDelete = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotation.Id);
            if (toDelete is not null) db.Quotations.Remove(toDelete);
            await db.SaveChangesAsync();
        }
    }

    [Theory]
    [InlineData(150, 100)]
    [InlineData(-10, 0)]
    public async Task Discount_is_clamped_to_0_100_range_on_update(decimal input, decimal expected)
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var created = await quotationSvc.CreateAsync(BaseRequest(false,
            new SaveQuotationGroupRequest
            {
                Name = "Group 1", SortOrder = 0,
                Items =
                [
                    new SaveQuotationItemRequest
                    {
                        ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit",
                        MaterialPrice = 1_000_000, ServicePrice = 0, SortOrder = 0,
                    },
                ],
            }));

        try
        {
            var updateRequest = BaseRequest(false,
                new SaveQuotationGroupRequest
                {
                    Id = created.Tabs[0].Groups[0].Id,
                    Name = "Group 1", SortOrder = 0,
                    Items =
                    [
                        new SaveQuotationItemRequest
                        {
                            ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit",
                            MaterialPrice = 1_000_000, ServicePrice = 0, SortOrder = 0,
                        },
                    ],
                });
            updateRequest.Tabs[0].Id = created.Tabs[0].Id;
            updateRequest.Discount = input;

            var updated = await quotationSvc.UpdateAsync(created.Id, updateRequest);

            updated!.Discount.Should().Be(expected);
        }
        finally
        {
            var toDelete = await db.Quotations.FirstOrDefaultAsync(q => q.Id == created.Id);
            if (toDelete is not null) db.Quotations.Remove(toDelete);
            await db.SaveChangesAsync();
        }
    }
}
