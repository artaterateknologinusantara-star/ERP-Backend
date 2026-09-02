using SynteraERP.Api.DTOs.DemoLead;

namespace SynteraERP.Api.Services.Interfaces;

public interface IDemoLeadService
{
    Task<List<DemoLeadDto>> ListAsync();
    Task<DemoLeadDto> CreateAsync(CreateDemoLeadRequest request);
    Task<DemoLeadDto?> UpdateStatusAsync(Guid id, UpdateDemoLeadStatusRequest request);
}
