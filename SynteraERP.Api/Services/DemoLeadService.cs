using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.DemoLead;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Services;

public class DemoLeadService : IDemoLeadService
{
    private readonly AppDbContext _db;

    public DemoLeadService(AppDbContext db) => _db = db;

    public async Task<List<DemoLeadDto>> ListAsync()
    {
        var data = await _db.DemoLeads.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return data.Select(ToDto).ToList();
    }

    public async Task<DemoLeadDto> CreateAsync(CreateDemoLeadRequest request)
    {
        var lead = new DemoLead
        {
            FullName       = request.FullName.Trim(),
            WhatsappNumber = request.WhatsappNumber.Trim(),
            CompanyEmail   = request.CompanyEmail.Trim(),
            CompanyName    = request.CompanyName.Trim(),
            Industry       = request.Industry.Trim(),
            Need           = request.Need,
            Notes          = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };

        _db.DemoLeads.Add(lead);
        await _db.SaveChangesAsync();

        return ToDto(lead);
    }

    public async Task<DemoLeadDto?> UpdateStatusAsync(Guid id, UpdateDemoLeadStatusRequest request)
    {
        var lead = await _db.DemoLeads.FindAsync(id);
        if (lead is null) return null;

        if (!Enum.TryParse<DemoLeadStatus>(request.Status, ignoreCase: true, out var status))
            throw new InvalidOperationException($"Status '{request.Status}' tidak valid.");

        lead.Status    = status;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(lead);
    }

    private static DemoLeadDto ToDto(DemoLead x) => new()
    {
        Id             = x.Id,
        FullName       = x.FullName,
        WhatsappNumber = x.WhatsappNumber,
        CompanyEmail   = x.CompanyEmail,
        CompanyName    = x.CompanyName,
        Industry       = x.Industry,
        Need           = x.Need,
        Notes          = x.Notes,
        Status         = x.Status.ToString(),
        CreatedAt      = x.CreatedAt,
    };
}
