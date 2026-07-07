using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/projects")]
public class ProjectController(AppDbContext db) : ControllerBase
{
    // ── DTOs ──────────────────────────────────────────────────────────────────

    public record ProjectListDto(Guid Id, string Code, string Name, string CustomerName,
        string? ProjectManagerName, string Status, int Progress, decimal Budget,
        DateOnly StartDate, DateOnly? EndDate, int TaskCount, DateTimeOffset CreatedAt);

    public record ProjectDetailDto(Guid Id, string Code, string Name,
        Guid CustomerId, string CustomerName,
        Guid? SalesOrderId, string? SalesOrderNo,
        Guid? ProjectManagerId, string? ProjectManagerName,
        string Status, int Progress, decimal Budget,
        DateOnly StartDate, DateOnly? EndDate, string? Notes,
        DateTimeOffset CreatedAt, List<TaskDto> Tasks);

    public record TaskDto(Guid Id, string Title, string? Description,
        Guid? AssignedToId, string? AssignedToName,
        string Status, string Priority, DateOnly? DueDate, int SortOrder);

    public record CreateProjectRequest(
        string Name, Guid CustomerId, Guid? SalesOrderId,
        Guid? ProjectManagerId, DateOnly StartDate, DateOnly? EndDate,
        decimal Budget, string? Notes);

    public record UpdateProjectRequest(
        string Name, Guid CustomerId, Guid? SalesOrderId,
        Guid? ProjectManagerId, DateOnly StartDate, DateOnly? EndDate,
        decimal Budget, int Progress, string Status, string? Notes);

    public record CreateTaskRequest(
        string Title, string? Description,
        Guid? AssignedToId, DateOnly? DueDate,
        string Priority, int SortOrder, string? Notes);

    public record UpdateTaskStatusRequest(string Status);

    public record ProjectStatsDto(int Total, int Running, int OnHold, int Completed, int Planning);

    public record ProjectCostDto(
        Guid   ProjectId,
        string ProjectName,
        string? SalesOrderNo,
        decimal Revenue,
        decimal ProcurementCost,
        decimal VendorPayment,
        decimal CustomerBilling,
        decimal CustomerPayment,
        decimal OutstandingAR,
        decimal OutstandingAP,
        decimal EstimatedMargin);

    // ── Endpoints ─────────────────────────────────────────────────────────────

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<ProjectStatsDto>>> Stats()
    {
        var groups = await db.Projects
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var stats = new ProjectStatsDto(
            Total:     groups.Sum(g => g.Count),
            Running:   groups.FirstOrDefault(g => g.Status == ProjectStatus.Running)?.Count ?? 0,
            OnHold:    groups.FirstOrDefault(g => g.Status == ProjectStatus.OnHold)?.Count ?? 0,
            Completed: groups.FirstOrDefault(g => g.Status == ProjectStatus.Completed)?.Count ?? 0,
            Planning:  groups.FirstOrDefault(g => g.Status == ProjectStatus.Planning)?.Count ?? 0
        );
        return Ok(ApiResponse<ProjectStatsDto>.Ok(stats));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ProjectListDto>>>> List([FromQuery] PaginationParams p)
    {
        var q = db.Projects
            .Include(x => x.Customer)
            .Include(x => x.ProjectManager)
            .Include(x => x.Tasks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(s) || x.Code.ToLower().Contains(s)
                           || x.Customer.Name.ToLower().Contains(s));
        }

        q = q.OrderByDescending(x => x.CreatedAt);
        var total = await q.CountAsync();
        var data  = await q.Skip(p.Skip).Take(p.PerPage)
            .Select(x => new ProjectListDto(x.Id, x.Code, x.Name,
                x.Customer.Name, x.ProjectManager != null ? x.ProjectManager.Name : null,
                x.Status.ToString(), x.Progress, x.Budget,
                x.StartDate, x.EndDate, x.Tasks.Count, x.CreatedAt))
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<ProjectListDto>>.Ok(
            PaginatedResponse<ProjectListDto>.Create(data, total, p.Page, p.PerPage)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectDetailDto>>> Get(Guid id)
    {
        var p = await db.Projects
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .Include(x => x.ProjectManager)
            .Include(x => x.Tasks.OrderBy(t => t.SortOrder))
                .ThenInclude(t => t.AssignedTo)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p is null) return NotFound(ApiResponse<ProjectDetailDto>.Fail("Project tidak ditemukan."));

        var dto = new ProjectDetailDto(p.Id, p.Code, p.Name,
            p.CustomerId, p.Customer.Name,
            p.SalesOrderId, p.SalesOrder?.No,
            p.ProjectManagerId, p.ProjectManager?.Name,
            p.Status.ToString(), p.Progress, p.Budget,
            p.StartDate, p.EndDate, p.Notes, p.CreatedAt,
            p.Tasks.Select(t => new TaskDto(t.Id, t.Title, t.Description,
                t.AssignedToId, t.AssignedTo?.Name,
                t.Status.ToString(), t.Priority.ToString(), t.DueDate, t.SortOrder)).ToList());

        return Ok(ApiResponse<ProjectDetailDto>.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProjectListDto>>> Create([FromBody] CreateProjectRequest req)
    {
        var code = await GenerateCodeAsync();
        var project = new Project
        {
            Code             = code,
            Name             = req.Name,
            CustomerId       = req.CustomerId,
            SalesOrderId     = req.SalesOrderId,
            ProjectManagerId = req.ProjectManagerId,
            StartDate        = req.StartDate,
            EndDate          = req.EndDate,
            Budget           = req.Budget,
            Notes            = req.Notes,
            Status           = ProjectStatus.Planning,
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        await db.Entry(project).Reference(x => x.Customer).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = project.Id },
            ApiResponse<ProjectListDto>.Ok(new ProjectListDto(project.Id, project.Code, project.Name,
                project.Customer.Name, null, project.Status.ToString(), 0, project.Budget,
                project.StartDate, project.EndDate, 0, project.CreatedAt),
                "Project berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateProjectRequest req)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is null) return NotFound(ApiResponse<object>.Fail("Project tidak ditemukan."));

        if (!Enum.TryParse<ProjectStatus>(req.Status, out var status))
            return BadRequest(ApiResponse<object>.Fail("Status tidak valid."));

        project.Name             = req.Name;
        project.CustomerId       = req.CustomerId;
        project.SalesOrderId     = req.SalesOrderId;
        project.ProjectManagerId = req.ProjectManagerId;
        project.StartDate        = req.StartDate;
        project.EndDate          = req.EndDate;
        project.Budget           = req.Budget;
        project.Progress         = Math.Clamp(req.Progress, 0, 100);
        project.Status           = status;
        project.Notes            = req.Notes;
        project.UpdatedAt        = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Project berhasil diperbarui."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is null) return NotFound(ApiResponse.Fail("Project tidak ditemukan."));
        project.IsDeleted = true;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Project berhasil dihapus."));
    }

    // ── Cost Monitoring ───────────────────────────────────────────────────────

    [HttpGet("{id:guid}/cost")]
    public async Task<ActionResult<ApiResponse<ProjectCostDto>>> GetCost(Guid id)
    {
        var project = await db.Projects
            .Include(x => x.SalesOrder)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null) return NotFound(ApiResponse<ProjectCostDto>.Fail("Project tidak ditemukan."));

        var soId    = project.SalesOrderId;
        var revenue = project.SalesOrder?.Total ?? 0;

        // PR linked to this SO (global query filter handles IsDeleted)
        var prQuery = soId.HasValue
            ? db.PurchaseRequests.Where(pr => pr.SalesOrderId == soId.Value)
            : db.PurchaseRequests.Where(_ => false);

        var procurementCost = await prQuery.SumAsync(pr => (decimal?)pr.Total) ?? 0;

        var prIds = soId.HasValue
            ? await prQuery.Select(pr => pr.Id).ToListAsync()
            : [];

        // PO payments via PR → PO → POPayment chain
        decimal vendorPayment = 0;
        if (prIds.Count > 0)
        {
            var poIds = await db.PurchaseOrders
                .Where(po => po.PurchaseRequestId.HasValue && prIds.Contains(po.PurchaseRequestId.Value))
                .Select(po => po.Id)
                .ToListAsync();

            if (poIds.Count > 0)
                vendorPayment = await db.POPayments
                    .Where(p => poIds.Contains(p.PurchaseOrderId))
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;
        }

        // Invoice data linked to SO
        var invQuery = soId.HasValue
            ? db.Invoices.Where(i => i.SalesOrderId == soId.Value)
            : db.Invoices.Where(_ => false);

        var customerBilling = await invQuery.SumAsync(i => (decimal?)i.Amount) ?? 0;
        var customerPayment = await invQuery.SumAsync(i => (decimal?)i.Paid)   ?? 0;

        return Ok(ApiResponse<ProjectCostDto>.Ok(new ProjectCostDto(
            ProjectId:        project.Id,
            ProjectName:      project.Name,
            SalesOrderNo:     project.SalesOrder?.No,
            Revenue:          revenue,
            ProcurementCost:  procurementCost,
            VendorPayment:    vendorPayment,
            CustomerBilling:  customerBilling,
            CustomerPayment:  customerPayment,
            OutstandingAR:    customerBilling - customerPayment,
            OutstandingAP:    procurementCost - vendorPayment,
            EstimatedMargin:  revenue - vendorPayment)));
    }

    // ── Tasks ─────────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/tasks")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> AddTask(Guid id, [FromBody] CreateTaskRequest req)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == id))
            return NotFound(ApiResponse<TaskDto>.Fail("Project tidak ditemukan."));

        if (!Enum.TryParse<ProjectTaskPriority>(req.Priority, out var priority))
            priority = ProjectTaskPriority.Medium;

        var task = new ProjectTask
        {
            ProjectId    = id,
            Title        = req.Title,
            Description  = req.Description,
            AssignedToId = req.AssignedToId,
            DueDate      = req.DueDate,
            Priority     = priority,
            SortOrder    = req.SortOrder,
            Notes        = req.Notes,
            Status       = ProjectTaskStatus.Todo,
        };
        db.ProjectTasks.Add(task);

        await db.SaveChangesAsync();
        if (task.AssignedToId.HasValue) await db.Entry(task).Reference(t => t.AssignedTo).LoadAsync();

        return Ok(ApiResponse<TaskDto>.Ok(new TaskDto(task.Id, task.Title, task.Description,
            task.AssignedToId, task.AssignedTo?.Name,
            task.Status.ToString(), task.Priority.ToString(), task.DueDate, task.SortOrder),
            "Task berhasil ditambahkan."));
    }

    [HttpPatch("{projectId:guid}/tasks/{taskId:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateTaskStatus(Guid projectId, Guid taskId, [FromBody] UpdateTaskStatusRequest req)
    {
        var task = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
        if (task is null) return NotFound(ApiResponse.Fail("Task tidak ditemukan."));

        if (!Enum.TryParse<ProjectTaskStatus>(req.Status, out var status))
            return BadRequest(ApiResponse.Fail("Status tidak valid."));

        task.Status    = status;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        // Recalculate project progress
        var allTasks  = await db.ProjectTasks.Where(t => t.ProjectId == projectId).ToListAsync();
        var doneTasks = allTasks.Count(t => t.Status == ProjectTaskStatus.Done);
        if (allTasks.Count > 0)
        {
            var project   = await db.Projects.FindAsync(projectId);
            if (project is not null)
            {
                project.Progress  = (int)Math.Round((double)doneTasks / allTasks.Count * 100);
                project.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        return Ok(ApiResponse.Ok("Status task berhasil diperbarui."));
    }

    [HttpDelete("{projectId:guid}/tasks/{taskId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteTask(Guid projectId, Guid taskId)
    {
        var task = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
        if (task is null) return NotFound(ApiResponse.Fail("Task tidak ditemukan."));
        task.IsDeleted = true;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Task berhasil dihapus."));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> GenerateCodeAsync()
    {
        var year  = DateTime.UtcNow.Year;
        var count = await db.Projects.CountAsync() + 1;
        return $"PRJ-{year}-{count:D3}";
    }
}
