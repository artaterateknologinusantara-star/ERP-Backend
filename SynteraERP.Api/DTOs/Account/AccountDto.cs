namespace SynteraERP.Api.DTOs.Account;

public class AccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? ParentAccountId { get; set; }
    public string NormalBalance { get; set; } = string.Empty;
    public bool IsControlAccount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<AccountDto> Children { get; set; } = [];
}

public class CreateAccountRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? ParentAccountId { get; set; }
    public string NormalBalance { get; set; } = string.Empty;
    public bool IsControlAccount { get; set; }
}

public class UpdateAccountRequest : CreateAccountRequest { }
