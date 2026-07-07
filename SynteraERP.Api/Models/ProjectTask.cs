using SynteraERP.Api.Models.Common;

namespace SynteraERP.Api.Models;

public class ProjectTask : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? AssignedToId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? DueDate { get; set; }
    public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.Todo;
    public ProjectTaskPriority Priority { get; set; } = ProjectTaskPriority.Medium;
    public int SortOrder { get; set; } = 0;
    public string? Notes { get; set; }

    public Project Project { get; set; } = null!;
    public User? AssignedTo { get; set; }
}

public enum ProjectTaskStatus { Todo, InProgress, Done, Cancelled }
public enum ProjectTaskPriority { Low, Medium, High }
