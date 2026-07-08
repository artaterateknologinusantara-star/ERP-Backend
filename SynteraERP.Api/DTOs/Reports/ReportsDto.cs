namespace SynteraERP.Api.DTOs.Reports;

public class IncomeStatementAccountRowDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class IncomeStatementDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<IncomeStatementAccountRowDto> Revenues { get; set; } = [];
    public List<IncomeStatementAccountRowDto> Expenses { get; set; } = [];
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetIncome { get; set; }
}

public class BalanceSheetAccountRowDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class BalanceSheetDto
{
    public DateTimeOffset AsOfDate { get; set; }
    public List<BalanceSheetAccountRowDto> Assets { get; set; } = [];
    public List<BalanceSheetAccountRowDto> Liabilities { get; set; } = [];
    public List<BalanceSheetAccountRowDto> Equities { get; set; } = [];
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquities { get; set; }
    public decimal Selisih { get; set; }
}

public class GeneralLedgerLineDto
{
    public DateTimeOffset Date { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public class GeneralLedgerDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string NormalBalance { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<GeneralLedgerLineDto> Lines { get; set; } = [];
    public decimal ClosingBalance { get; set; }
}
