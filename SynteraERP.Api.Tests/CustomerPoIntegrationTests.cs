using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using SynteraERP.Api.Data;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new System.Security.Claims.Claim("sub", "20000000-0000-0000-0000-000000000001"), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestUser") };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class CustomerPoIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CustomerPoIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient client, IServiceProvider services) CreateClientWithScratchSqlServer()
    {
        var connectionString = "Server=localhost,1433;Database=SynteraERP_Scratch;User Id=sa;Password=DevgvImMkAaOBHs4CP5kWRsLLyM!9q;TrustServerCertificate=True;Encrypt=False;";

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // replace DbContext with SQL Server scratch DB
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));

                // add test auth
                services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                services.AddAuthorization();

                // No auto seeding in Testing environment; tests will control migrations.
            });
        });

        var client = factory.CreateClient();
        var services = factory.Services;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        return (client, services);
    }

    [Fact]
    public async Task CustomerPo_UpdateNumber_workflow_and_history_and_validations()
    {
        // Arrange
        var (client, services) = CreateClientWithScratchSqlServer();

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Apply migrations up to before target migration
        var all = db.Database.GetMigrations().ToList();
        var target = "20260820212956_AddCustomerPoHistory";
        var idx = all.IndexOf(target);
        var before = idx > 0 ? all[idx - 1] : "0";
        var migrator = db.GetService<IMigrator>();

        if (before != "0") migrator.Migrate(before);
        else db.Database.EnsureCreated();

        // Seed minimal reference data (customers, suppliers, numbering) into scratch DB
        await SynteraERP.Api.Data.CustomerSeeder.SeedAsync(db);
        await SynteraERP.Api.Data.ItemMasterSeeder.SeedAsync(db);
        await SynteraERP.Api.Data.SupplierSeeder.SeedAsync(db);
        await SynteraERP.Api.Data.NumberingConfigSeeder.SeedAsync(db);

        // Snapshot numberingconfigs before
        var beforeList = await db.NumberingConfigs.Select(n => new { n.DocType, n.Prefix, n.LastNumber }).ToListAsync();

        // Now migrate to target (apply the migration under test)
        migrator.Migrate(target);

        // Snapshot after
        var afterList = await db.NumberingConfigs.Select(n => new { n.DocType, n.Prefix, n.LastNumber }).ToListAsync();

        // Compare before/after for Prefix and LastNumber equality
        beforeList.Should().BeEquivalentTo(afterList, options => options.ComparingByMembers<object>());

        // Seed a Quotation approved (attach to seeded customer and admin sales user)
        var seededCustomerId = new Guid("C0000000-0000-0000-0000-000000000001");
        var seededAdminId = new Guid("20000000-0000-0000-0000-000000000001");
        var q = new Quotation { Id = Guid.NewGuid(), No = "TST-Q-" + Guid.NewGuid().ToString().Substring(0,6), CustomerId = seededCustomerId, ProjectName = "P", SalesId = seededAdminId, GrandTotal = 1000, Status = QuotationStatus.Disetujui };
        q.Date = DateOnly.FromDateTime(DateTime.UtcNow);
        q.ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow + TimeSpan.FromDays(30));
        db.Quotations.Add(q);
        await db.SaveChangesAsync();

        // Create CustomerPO
        var cpo = new CustomerPO { Id = Guid.NewGuid(), QuotationId = q.Id, PoNo = "CPO-INIT", PoDate = DateOnly.FromDateTime(DateTime.UtcNow), Amount = 1000 };
        db.CustomerPOs.Add(cpo);
        await db.SaveChangesAsync();

        // Act: update number twice via service
        var svc = scope.ServiceProvider.GetRequiredService<SynteraERP.Api.Services.Interfaces.ICustomerPoService>();

        var updated1 = await svc.UpdateNumberAsync(cpo.Id, "CPO-EDIT-1", "test1");
        updated1.PoNo.Should().Be("CPO-EDIT-1");

        var updated2 = await svc.UpdateNumberAsync(cpo.Id, "CPO-EDIT-2", "test2");
        updated2.PoNo.Should().Be("CPO-EDIT-2");

        // Verify history entries
        var histories = await db.CustomerPoHistories.Where(h => h.CustomerPoId == cpo.Id).OrderBy(h => h.ChangedAt).ToListAsync();
        histories.Should().HaveCount(2);
        histories[0].OldPoNo.Should().Be("CPO-INIT");
        histories[0].NewPoNo.Should().Be("CPO-EDIT-1");
        histories[1].OldPoNo.Should().Be("CPO-EDIT-1");
        histories[1].NewPoNo.Should().Be("CPO-EDIT-2");
        (histories[0].ChangedAt < histories[1].ChangedAt).Should().BeTrue();

        // Validation: empty newPoNo -> ArgumentException handled via service (throws ArgumentException)
        await FluentActions.Invoking(() => svc.UpdateNumberAsync(cpo.Id, "   ", null)).Should().ThrowAsync<ArgumentException>().WithMessage("newPoNo is required.");

        // Validation: identical PoNo -> InvalidOperationException
        await FluentActions.Invoking(() => svc.UpdateNumberAsync(cpo.Id, "CPO-EDIT-2", null)).Should().ThrowAsync<InvalidOperationException>().WithMessage("Nomor PO baru sama dengan nomor saat ini.");

        // Non-existent id -> KeyNotFoundException
        await FluentActions.Invoking(() => svc.UpdateNumberAsync(Guid.NewGuid(), "X", null)).Should().ThrowAsync<KeyNotFoundException>();

        // Test GET /history endpoint ordering (DESC)
        var http = client;
        var resp = await http.GetAsync($"/api/customer-pos/{cpo.Id}/history");
        resp.EnsureSuccessStatusCode();
        var payload = JsonSerializer.Deserialize<JsonElement>(await resp.Content.ReadAsStringAsync());
        // payload.data is array, first element should be latest (CPO-EDIT-2)
        var data = payload.GetProperty("data");
        data[0].GetProperty("newPoNo").GetString().Should().Be("CPO-EDIT-2");

        // Cleanup
    }
}
