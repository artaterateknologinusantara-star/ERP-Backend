namespace SynteraERP.Api.DTOs.Customer;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Npwp { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Npwp { get; set; }
}

public class UpdateCustomerRequest : CreateCustomerRequest { }
