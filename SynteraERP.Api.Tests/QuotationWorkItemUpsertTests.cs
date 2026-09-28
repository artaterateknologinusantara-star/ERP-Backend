using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Quotation;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Tests;

// SaveQuotationGroupRequest.WorkItems (nested WorkItems/WorkDetails, saved together with the rest
// of "Submit Penawaran") must be upserted by Id — INSERT when Id is null, UPDATE in place when Id
// matches an existing row, DELETE only rows whose Id disappeared from the payload — the SAME
// pattern as UpsertTabs/UpsertGroups already use, NOT a Clear()+recreate. A past incident already
// proved that recreate-on-every-save silently orphans data FK'd to these rows (see
// UpsertGroups' own header comment in QuotationService.cs and QuotationCivilMeTotalsTests). This
// suite locks in that same guarantee for the new nested payload path, including attachment-file
// cleanup on implicit delete.
public class QuotationWorkItemUpsertTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededAdminId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCustomerId = new("C0000000-0000-0000-0000-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public QuotationWorkItemUpsertTests(WebApplicationFactory<Program> factory)
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

    private static IFormFile MakePngFile(string fileName = "photo.png")
    {
        // Only the 8-byte PNG signature matters to UploadWorkDetailAttachmentAsync's magic-byte
        // check — padding bytes make it a well-formed *file* without needing a decodable image.
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName) { Headers = new HeaderDictionary(), ContentType = "image/png" };
    }

    private static SaveQuotationRequest BaseRequest(string projectName, SaveQuotationTabRequest tab) => new()
    {
        CustomerId = SeededCustomerId,
        SalesId = SeededAdminId,
        ProjectName = projectName,
        Date = DateOnly.FromDateTime(DateTime.UtcNow),
        ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
        Discount = 0,
        TaxRate = 11,
        IsCivilMeMode = true,
        Tabs = [tab],
    };

    [Fact]
    public async Task Save_inserts_new_WorkItem_and_WorkDetail_nested_in_group_payload()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var svc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var request = BaseRequest("Test WorkItem Insert " + Guid.NewGuid().ToString("N")[..6], new SaveQuotationTabRequest
        {
            Label = "Tab 1",
            SortOrder = 0,
            Groups =
            [
                new SaveQuotationGroupRequest
                {
                    Name = "Pekerjaan Sipil",
                    SortOrder = 0,
                    WorkItems =
                    [
                        new SaveQuotationWorkItemRequest
                        {
                            Name = "Pemasangan Kabel",
                            SortOrder = 0,
                            WorkDetails =
                            [
                                new SaveQuotationWorkDetailRequest
                                {
                                    Name = "Kabel NYY 4x6mm", Spesifikasi = "Supreme",
                                    Volume = 5, Unit = "meter", MaterialPrice = 200_000, SortOrder = 0,
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        var created = await svc.CreateAsync(request);

        created.Tabs[0].Groups[0].WorkItems.Should().ContainSingle()
            .Which.Name.Should().Be("Pemasangan Kabel");
        created.Tabs[0].Groups[0].WorkItems[0].WorkDetails.Should().ContainSingle()
            .Which.Name.Should().Be("Kabel NYY 4x6mm");
        created.Tabs[0].Groups[0].WorkItems[0].WorkDetails[0].Id.Should().NotBe(Guid.Empty);

        await CleanupAsync(db, created.Id);
    }

    [Fact]
    public async Task Update_matches_existing_WorkItem_and_WorkDetail_by_Id_instead_of_recreating()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var svc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var request = BaseRequest("Test WorkItem Update " + Guid.NewGuid().ToString("N")[..6], new SaveQuotationTabRequest
        {
            Label = "Tab 1",
            SortOrder = 0,
            Groups =
            [
                new SaveQuotationGroupRequest
                {
                    Name = "Pekerjaan Sipil",
                    SortOrder = 0,
                    WorkItems =
                    [
                        new SaveQuotationWorkItemRequest
                        {
                            Name = "Pemasangan Kabel",
                            SortOrder = 0,
                            WorkDetails =
                            [
                                new SaveQuotationWorkDetailRequest
                                {
                                    Name = "Kabel NYY 4x6mm", Spesifikasi = "Supreme",
                                    Volume = 5, Unit = "meter", MaterialPrice = 200_000, SortOrder = 0,
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        var created = await svc.CreateAsync(request);
        var group = created.Tabs[0].Groups[0];
        var workItemId = group.WorkItems[0].Id;
        var workDetailId = group.WorkItems[0].WorkDetails[0].Id;

        // Second save: same Ids, changed values — must UPDATE in place, not delete+recreate.
        var updateRequest = BaseRequest(created.ProjectName, new SaveQuotationTabRequest
        {
            Id = created.Tabs[0].Id,
            Label = "Tab 1",
            SortOrder = 0,
            Groups =
            [
                new SaveQuotationGroupRequest
                {
                    Id = group.Id,
                    Name = "Pekerjaan Sipil",
                    SortOrder = 0,
                    WorkItems =
                    [
                        new SaveQuotationWorkItemRequest
                        {
                            Id = workItemId,
                            Name = "Pemasangan Kabel (revisi)",
                            SortOrder = 1,
                            WorkDetails =
                            [
                                new SaveQuotationWorkDetailRequest
                                {
                                    Id = workDetailId,
                                    Name = "Kabel NYY 4x6mm", Spesifikasi = "Supreme",
                                    Volume = 10, Unit = "meter", MaterialPrice = 250_000, SortOrder = 0,
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        var updated = await svc.UpdateAsync(created.Id, updateRequest);

        var updatedWorkItem = updated!.Tabs[0].Groups[0].WorkItems.Should().ContainSingle().Subject;
        updatedWorkItem.Id.Should().Be(workItemId); // same row, not a new Guid
        updatedWorkItem.Name.Should().Be("Pemasangan Kabel (revisi)");
        updatedWorkItem.SortOrder.Should().Be(1);

        var updatedDetail = updatedWorkItem.WorkDetails.Should().ContainSingle().Subject;
        updatedDetail.Id.Should().Be(workDetailId); // same row, not a new Guid
        updatedDetail.Volume.Should().Be(10);
        updatedDetail.MaterialPrice.Should().Be(250_000);

        await CleanupAsync(db, created.Id);
    }

    [Fact]
    public async Task Update_deletes_WorkDetail_missing_from_payload_and_cleans_up_its_attachment_file()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var svc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var request = BaseRequest("Test WorkDetail Delete " + Guid.NewGuid().ToString("N")[..6], new SaveQuotationTabRequest
        {
            Label = "Tab 1",
            SortOrder = 0,
            Groups =
            [
                new SaveQuotationGroupRequest
                {
                    Name = "Pekerjaan Sipil",
                    SortOrder = 0,
                    WorkItems =
                    [
                        new SaveQuotationWorkItemRequest
                        {
                            Name = "Pemasangan Kabel",
                            SortOrder = 0,
                            WorkDetails =
                            [
                                new SaveQuotationWorkDetailRequest
                                {
                                    Name = "Kabel yang akan dihapus", Volume = 5, Unit = "meter",
                                    MaterialPrice = 200_000, SortOrder = 0,
                                },
                                new SaveQuotationWorkDetailRequest
                                {
                                    Name = "Kabel yang bertahan", Volume = 3, Unit = "meter",
                                    MaterialPrice = 100_000, SortOrder = 1,
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        var created = await svc.CreateAsync(request);
        var group = created.Tabs[0].Groups[0];
        var workItemId = group.WorkItems[0].Id;
        var toDeleteId = group.WorkItems[0].WorkDetails.Single(d => d.Name == "Kabel yang akan dihapus").Id;
        var toKeepId = group.WorkItems[0].WorkDetails.Single(d => d.Name == "Kabel yang bertahan").Id;

        // Upload a real attachment onto the WorkDetail that is about to be implicitly deleted.
        var attachment = await svc.UploadWorkDetailAttachmentAsync(toDeleteId, MakePngFile());
        var attachmentRow = await db.QuotationWorkDetailAttachments.AsNoTracking()
            .FirstAsync(a => a.Id == attachment.Id);
        var storedFullPath = Path.Combine(env.ContentRootPath, "uploads", attachmentRow.FilePath);
        File.Exists(storedFullPath).Should().BeTrue("attachment upload should have written the file to disk");

        // Save again, omitting the "Kabel yang akan dihapus" WorkDetail — Id missing from payload
        // means implicit delete.
        var updateRequest = BaseRequest(created.ProjectName, new SaveQuotationTabRequest
        {
            Id = created.Tabs[0].Id,
            Label = "Tab 1",
            SortOrder = 0,
            Groups =
            [
                new SaveQuotationGroupRequest
                {
                    Id = group.Id,
                    Name = "Pekerjaan Sipil",
                    SortOrder = 0,
                    WorkItems =
                    [
                        new SaveQuotationWorkItemRequest
                        {
                            Id = workItemId,
                            Name = "Pemasangan Kabel",
                            SortOrder = 0,
                            WorkDetails =
                            [
                                new SaveQuotationWorkDetailRequest
                                {
                                    Id = toKeepId,
                                    Name = "Kabel yang bertahan", Volume = 3, Unit = "meter",
                                    MaterialPrice = 100_000, SortOrder = 1,
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        var updated = await svc.UpdateAsync(created.Id, updateRequest);

        var survivingWorkItem = updated!.Tabs[0].Groups[0].WorkItems.Should().ContainSingle().Subject;
        survivingWorkItem.WorkDetails.Should().ContainSingle()
            .Which.Id.Should().Be(toKeepId);

        // Row gone from DB.
        (await db.QuotationWorkDetails.AsNoTracking().AnyAsync(d => d.Id == toDeleteId)).Should().BeFalse();
        // File gone from disk — the core risk this test guards against.
        File.Exists(storedFullPath).Should().BeFalse("implicit delete must clean up the attachment file, not leave it orphaned");

        await CleanupAsync(db, created.Id);
    }

    [Fact]
    public async Task Update_deletes_WorkItem_missing_from_payload_and_cleans_up_all_its_WorkDetail_attachments()
    {
        var services = CreateScratchServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        await db.Database.MigrateAsync();
        await CustomerSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);

        var svc = scope.ServiceProvider.GetRequiredService<IQuotationService>();

        var request = BaseRequest("Test WorkItem Delete " + Guid.NewGuid().ToString("N")[..6], new SaveQuotationTabRequest
        {
            Label = "Tab 1",
            SortOrder = 0,
            Groups =
            [
                new SaveQuotationGroupRequest
                {
                    Name = "Pekerjaan Sipil",
                    SortOrder = 0,
                    WorkItems =
                    [
                        new SaveQuotationWorkItemRequest
                        {
                            Name = "Pemasangan Kabel (akan dihapus)",
                            SortOrder = 0,
                            WorkDetails =
                            [
                                new SaveQuotationWorkDetailRequest
                                {
                                    Name = "Kabel A", Volume = 5, Unit = "meter",
                                    MaterialPrice = 200_000, SortOrder = 0,
                                },
                            ],
                        },
                    ],
                },
            ],
        });

        var created = await svc.CreateAsync(request);
        var group = created.Tabs[0].Groups[0];
        var workDetailId = group.WorkItems[0].WorkDetails[0].Id;

        var attachment = await svc.UploadWorkDetailAttachmentAsync(workDetailId, MakePngFile());
        var attachmentRow = await db.QuotationWorkDetailAttachments.AsNoTracking()
            .FirstAsync(a => a.Id == attachment.Id);
        var storedFullPath = Path.Combine(env.ContentRootPath, "uploads", attachmentRow.FilePath);
        File.Exists(storedFullPath).Should().BeTrue();

        // Save again with WorkItems entirely empty — the whole WorkItem (and its WorkDetail +
        // attachment) is implicitly deleted.
        var updateRequest = BaseRequest(created.ProjectName, new SaveQuotationTabRequest
        {
            Id = created.Tabs[0].Id,
            Label = "Tab 1",
            SortOrder = 0,
            Groups =
            [
                new SaveQuotationGroupRequest
                {
                    Id = group.Id,
                    Name = "Pekerjaan Sipil",
                    SortOrder = 0,
                    WorkItems = [],
                },
            ],
        });

        var updated = await svc.UpdateAsync(created.Id, updateRequest);

        updated!.Tabs[0].Groups[0].WorkItems.Should().BeEmpty();
        (await db.QuotationWorkItems.AsNoTracking().AnyAsync(w => w.GroupId == group.Id)).Should().BeFalse();
        File.Exists(storedFullPath).Should().BeFalse("deleting the whole WorkItem must also clean up its WorkDetails' attachment files");

        await CleanupAsync(db, created.Id);
    }

    private static async Task CleanupAsync(AppDbContext db, Guid quotationId)
    {
        var toDelete = await db.Quotations.FirstOrDefaultAsync(q => q.Id == quotationId);
        if (toDelete is not null)
        {
            db.Quotations.Remove(toDelete);
            await db.SaveChangesAsync();
        }
    }
}
