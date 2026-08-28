using SynteraERP.Api.DTOs.Common;

namespace SynteraERP.Api.DTOs.Supplier;

public class SupplierParams : PaginationParams
{
    public bool? IsActive { get; set; }
}

public class SupplierDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Npwp { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateSupplierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Npwp { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
}

public class UpdateSupplierRequest : CreateSupplierRequest { }
