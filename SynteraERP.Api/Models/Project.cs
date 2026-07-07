using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class Project : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? ProjectManagerId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal Budget { get; set; } = 0;
    public int Progress { get; set; } = 0;
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public string? Notes { get; set; }

    public Customer Customer { get; set; } = null!;
    public SalesOrder? SalesOrder { get; set; }
    public User? ProjectManager { get; set; }
    public ICollection<ProjectTask> Tasks { get; set; } = [];
}

public enum ProjectStatus { Planning, Running, OnHold, Completed, Cancelled }
