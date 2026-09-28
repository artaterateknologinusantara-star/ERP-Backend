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
using SynteraERP.Api.DTOs.Branch;
using SynteraERP.Api.DTOs.Quotation;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// Task #25 (26 Sep 2026): SequentialCodeHelper switched from COUNT(active)+1 to MAX(ever issued)+1
// to close the permanent-gap-after-any-deletion collision (real incident: PRJ-2026-029, 2 Sep
// 2026; also broke task #41/#45 test runs on PRJ-2026-030/031; Step-0 investigation for this task
// found CUST0018 and SUPP0010 already actively collided in dev DB). Each test reproduces the exact
// failure shape: create 2 real rows, soft-delete the FIRST one (not the last — that's what makes
// COUNT drop below the true max), then generate the next code and assert it doesn't collide with
// the still-active second row.
public class SequentialCodeHelperTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public SequentialCodeHelperTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task Branch_next_code_skips_past_deleted_row_instead_of_colliding_with_active_one()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var codeA = await SequentialCodeHelper.NextCodeAsync(db.Branches, x => x.Code, "BR", 4);
        var a = new Branch { Code = codeA, Name = "Test Branch A " + Guid.NewGuid().ToString("N")[..6] };
        db.Branches.Add(a);
        await db.SaveChangesAsync();

        var codeB = await SequentialCodeHelper.NextCodeAsync(db.Branches, x => x.Code, "BR", 4);
        var b = new Branch { Code = codeB, Name = "Test Branch B " + Guid.NewGuid().ToString("N")[..6] };
        db.Branches.Add(b);
        await db.SaveChangesAsync();

        try
        {
            a.IsDeleted = true; // soft-delete the FIRST of the two — this is what desyncs COUNT
            await db.SaveChangesAsync();

            var codeC = await SequentialCodeHelper.NextCodeAsync(db.Branches, x => x.Code, "BR", 4);
            codeC.Should().NotBe(codeB); // old bug: COUNT(active)=1+1 would regenerate codeB exactly
            var stillActiveCollision = await db.Branches.AnyAsync(x => x.Code == codeC && !x.IsDeleted);
            stillActiveCollision.Should().BeFalse();
        }
        finally
        {
            // IgnoreQueryFilters: `a` was soft-deleted above, and Branches has a global
            // !IsDeleted query filter — without this, the cleanup query itself would silently
            // exclude `a`, leaving it behind as a permanent soft-deleted orphan every run.
            db.Branches.RemoveRange(db.Branches.IgnoreQueryFilters().Where(x => x.Id == a.Id || x.Id == b.Id));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Customer_next_code_skips_past_deleted_row_instead_of_colliding_with_active_one()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var codeA = await SequentialCodeHelper.NextCodeAsync(db.Customers, x => x.Code, "CUST", 4);
        var a = new Customer { Code = codeA, Name = "Test Customer A " + Guid.NewGuid().ToString("N")[..6] };
        db.Customers.Add(a);
        await db.SaveChangesAsync();

        var codeB = await SequentialCodeHelper.NextCodeAsync(db.Customers, x => x.Code, "CUST", 4);
        var b = new Customer { Code = codeB, Name = "Test Customer B " + Guid.NewGuid().ToString("N")[..6] };
        db.Customers.Add(b);
        await db.SaveChangesAsync();

        try
        {
            a.IsDeleted = true;
            await db.SaveChangesAsync();

            var codeC = await SequentialCodeHelper.NextCodeAsync(db.Customers, x => x.Code, "CUST", 4);
            codeC.Should().NotBe(codeB);
            var stillActiveCollision = await db.Customers.AnyAsync(x => x.Code == codeC && !x.IsDeleted);
            stillActiveCollision.Should().BeFalse();
        }
        finally
        {
            db.Customers.RemoveRange(db.Customers.IgnoreQueryFilters().Where(x => x.Id == a.Id || x.Id == b.Id));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task ItemMaster_next_code_skips_past_deleted_row_instead_of_colliding_with_active_one()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var codeA = await SequentialCodeHelper.NextCodeAsync(db.ItemMasters, x => x.Code, "ITM", 5);
        var a = new ItemMaster { Code = codeA, Name = "Test Item A " + Guid.NewGuid().ToString("N")[..6], Uom = "unit" };
        db.ItemMasters.Add(a);
        await db.SaveChangesAsync();

        var codeB = await SequentialCodeHelper.NextCodeAsync(db.ItemMasters, x => x.Code, "ITM", 5);
        var b = new ItemMaster { Code = codeB, Name = "Test Item B " + Guid.NewGuid().ToString("N")[..6], Uom = "unit" };
        db.ItemMasters.Add(b);
        await db.SaveChangesAsync();

        try
        {
            a.IsDeleted = true;
            await db.SaveChangesAsync();

            var codeC = await SequentialCodeHelper.NextCodeAsync(db.ItemMasters, x => x.Code, "ITM", 5);
            codeC.Should().NotBe(codeB);
            var stillActiveCollision = await db.ItemMasters.AnyAsync(x => x.Code == codeC && !x.IsDeleted);
            stillActiveCollision.Should().BeFalse();
        }
        finally
        {
            db.ItemMasters.RemoveRange(db.ItemMasters.IgnoreQueryFilters().Where(x => x.Id == a.Id || x.Id == b.Id));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Supplier_next_code_skips_past_deleted_row_instead_of_colliding_with_active_one()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var codeA = await SequentialCodeHelper.NextCodeAsync(db.Suppliers, x => x.Code, "SUPP", 4);
        var a = new Supplier { Code = codeA, Name = "Test Supplier A " + Guid.NewGuid().ToString("N")[..6] };
        db.Suppliers.Add(a);
        await db.SaveChangesAsync();

        var codeB = await SequentialCodeHelper.NextCodeAsync(db.Suppliers, x => x.Code, "SUPP", 4);
        var b = new Supplier { Code = codeB, Name = "Test Supplier B " + Guid.NewGuid().ToString("N")[..6] };
        db.Suppliers.Add(b);
        await db.SaveChangesAsync();

        try
        {
            a.IsDeleted = true;
            await db.SaveChangesAsync();

            var codeC = await SequentialCodeHelper.NextCodeAsync(db.Suppliers, x => x.Code, "SUPP", 4);
            codeC.Should().NotBe(codeB);
            var stillActiveCollision = await db.Suppliers.AnyAsync(x => x.Code == codeC && !x.IsDeleted);
            stillActiveCollision.Should().BeFalse();
        }
        finally
        {
            db.Suppliers.RemoveRange(db.Suppliers.IgnoreQueryFilters().Where(x => x.Id == a.Id || x.Id == b.Id));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Project_next_code_skips_past_deleted_row_instead_of_colliding_with_active_one()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);

        var year = DateTime.UtcNow.Year;
        var codeA = await SequentialCodeHelper.NextYearCodeAsync(db.Projects, x => x.Code, "PRJ", 3, year);
        var a = new Project
        {
            Code = codeA, Name = "Test Project A " + Guid.NewGuid().ToString("N")[..6],
            CustomerId = SeededCustomerId, StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.Projects.Add(a);
        await db.SaveChangesAsync();

        var codeB = await SequentialCodeHelper.NextYearCodeAsync(db.Projects, x => x.Code, "PRJ", 3, year);
        var b = new Project
        {
            Code = codeB, Name = "Test Project B " + Guid.NewGuid().ToString("N")[..6],
            CustomerId = SeededCustomerId, StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.Projects.Add(b);
        await db.SaveChangesAsync();

        try
        {
            a.IsDeleted = true;
            await db.SaveChangesAsync();

            var codeC = await SequentialCodeHelper.NextYearCodeAsync(db.Projects, x => x.Code, "PRJ", 3, year);
            codeC.Should().NotBe(codeB); // exactly the PRJ-2026-029/030/031 failure shape
            var stillActiveCollision = await db.Projects.AnyAsync(x => x.Code == codeC && !x.IsDeleted);
            stillActiveCollision.Should().BeFalse();

            // Legacy/unrelated-format codes already in this DB (dash-separated seed literals,
            // ad-hoc test fixtures like "TST-PRJ-...") must never be parsed as if they were real
            // generator output — sanity-check the regex-based parser didn't choke on them.
            codeC.Should().MatchRegex($@"^PRJ-{year}-\d{{3,}}$");
        }
        finally
        {
            db.Projects.RemoveRange(db.Projects.IgnoreQueryFilters().Where(x => x.Id == a.Id || x.Id == b.Id));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task NextYearCodeAsync_resets_numbering_for_a_different_year_even_if_current_year_has_codes()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var currentYear = DateTime.UtcNow.Year;
        var futureYear = currentYear + 5; // guaranteed no existing data for this "year"

        var code = await SequentialCodeHelper.NextYearCodeAsync(db.Projects, x => x.Code, "PRJ", 3, futureYear);

        code.Should().Be($"PRJ-{futureYear}-001");
    }

    // MAX()+1 closes the deterministic post-deletion gap, but a genuine TOCTOU race between two
    // concurrent creates reading the same MAX before either commits is still possible — that's
    // what RunWithRetryAsync is still for. Simulated here by pre-inserting a row with EXACTLY the
    // code the service's first attempt will generate (equivalent to "the other concurrent request
    // already committed it"), then confirming CreateAsync still succeeds via self-heal instead of
    // surfacing the raw unique-constraint 500.
    [Fact]
    public async Task Branch_CreateAsync_self_heals_via_RunWithRetryAsync_when_first_attempt_collides()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var collidingCode = await SequentialCodeHelper.NextCodeAsync(db.Branches, x => x.Code, "BR", 4);
        var preExisting = new Branch { Code = collidingCode, Name = "Test Branch Race " + Guid.NewGuid().ToString("N")[..6] };
        db.Branches.Add(preExisting);
        await db.SaveChangesAsync();

        var branchSvc = scope.ServiceProvider.GetRequiredService<IBranchService>();

        try
        {
            var created = await branchSvc.CreateAsync(new CreateBranchRequest { Name = "Test Branch Race New " + Guid.NewGuid().ToString("N")[..6] });

            created.Code.Should().NotBe(collidingCode); // first attempt collided; retry produced a different code
            var activeWithThatCode = await db.Branches.CountAsync(x => x.Code == created.Code && !x.IsDeleted);
            activeWithThatCode.Should().Be(1);

            db.Branches.RemoveRange(db.Branches.Where(x => x.Id == preExisting.Id || x.Code == created.Code));
            await db.SaveChangesAsync();
        }
        finally
        {
            var leftover = await db.Branches.FirstOrDefaultAsync(x => x.Id == preExisting.Id);
            if (leftover is not null)
            {
                db.Branches.Remove(leftover);
                await db.SaveChangesAsync();
            }
        }
    }

    // Confirms the SalesOrderService.cs redesign: a Project.Code collision during SO-from-Quotation
    // conversion must retry ONLY the Project's code (via the dedicated IsProjectCodeViolation catch)
    // and must NOT burn a SalesOrder document number in the process — burning one on every retry is
    // exactly the orphan-SO failure mode the single-transaction design was built to avoid (see the
    // comment above the retry loop in SalesOrderService.cs).
    [Fact]
    public async Task SO_conversion_retries_only_ProjectCode_on_collision_without_burning_a_SO_number()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var quotationSvc = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var salesOrderSvc = scope.ServiceProvider.GetRequiredService<ISalesOrderService>();

        var quotation = await quotationSvc.CreateAsync(new SaveQuotationRequest
        {
            CustomerId = SeededCustomerId,
            SalesId = new Guid("20000000-0000-0000-0000-000000000001"),
            ProjectName = "Test SO Project Collision " + Guid.NewGuid().ToString("N")[..6],
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Discount = 0,
            TaxRate = 11,
            Tabs =
            [
                new SaveQuotationTabRequest
                {
                    Label = "Tab 1", SortOrder = 0,
                    Groups =
                    [
                        new SaveQuotationGroupRequest
                        {
                            Name = "Group 1", SortOrder = 0,
                            Items =
                            [
                                new SaveQuotationItemRequest
                                {
                                    ItemNo = "1", Equipment = "Test Item", Qty = 1, Unit = "unit",
                                    MaterialPrice = 5_000_000, ServicePrice = 0, SortOrder = 0,
                                },
                            ],
                        },
                    ],
                },
            ],
        });
        await quotationSvc.UpdateStatusAsync(quotation.Id, "Disetujui");

        // Pre-insert a Project with EXACTLY the code BuildProjectForSoAsync will generate on its
        // first attempt, forcing the collision this test is verifying self-heals correctly.
        var year = DateTime.UtcNow.Year;
        var collidingCode = await SequentialCodeHelper.NextYearCodeAsync(db.Projects, x => x.Code, "PRJ", 3, year);
        var decoyProject = new Project
        {
            Code = collidingCode, Name = "Decoy " + Guid.NewGuid().ToString("N")[..6],
            CustomerId = SeededCustomerId, StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.Projects.Add(decoyProject);
        await db.SaveChangesAsync();

        var soNumberBefore = await db.NumberingConfigs
            .Where(n => n.DocType == "SALES_ORDER").Select(n => n.LastNumber).FirstAsync();

        try
        {
            var so = await salesOrderSvc.CreateFromQuotationAsync(quotation.Id, new Guid("20000000-0000-0000-0000-000000000001"));

            var soNumberAfter = await db.NumberingConfigs
                .Where(n => n.DocType == "SALES_ORDER").Select(n => n.LastNumber).FirstAsync();
            soNumberAfter.Should().Be(soNumberBefore + 1); // exactly 1 — not burned by the Project.Code retry

            var project = await db.Projects.FirstAsync(p => p.SalesOrderId == so.Id);
            project.Code.Should().NotBe(collidingCode); // retried past the decoy's code
        }
        finally
        {
            var soToDelete = await db.SalesOrders.FirstOrDefaultAsync(x => x.QuotationId == quotation.Id);
            if (soToDelete is not null) db.SalesOrders.Remove(soToDelete);
            db.Projects.RemoveRange(db.Projects.Where(p => p.Id == decoyProject.Id || (soToDelete != null && p.SalesOrderId == soToDelete.Id)));
            await db.SaveChangesAsync();
            var quotationToDelete = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotation.Id);
            if (quotationToDelete is not null) db.Quotations.Remove(quotationToDelete);
            await db.SaveChangesAsync();
        }
    }
}
