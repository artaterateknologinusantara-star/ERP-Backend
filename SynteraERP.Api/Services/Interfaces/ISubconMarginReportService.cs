using SynteraERP.Api.DTOs.Reports;

namespace SynteraERP.Api.Services.Interfaces;

public interface ISubconMarginReportService
{
    Task<SubconMarginByVendorReportDto> GetByVendorAsync(DateOnly? startDate, DateOnly? endDate, Guid? supplierId);
    Task<SubconMarginByQuotationReportDto> GetByQuotationAsync(DateOnly? startDate, DateOnly? endDate, Guid? supplierId);
}
