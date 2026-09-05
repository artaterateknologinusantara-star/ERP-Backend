using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Data;
using SynteraERP.Api.DTOs.Common;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services.Interfaces;

namespace SynteraERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/projects")]
public class ProjectController(AppDbContext db, IJournalPostingService journalPostingService) : ControllerBase
{
    // ── DTOs ──────────────────────────────────────────────────────────────────

    public class ProjectQueryParams : PaginationParams
    {
        public string? Status { get; set; }
    }

    public record ProjectListDto(Guid Id, string Code, string Name, string CustomerName,
        string? ProjectManagerName, string Status, int Progress, decimal Budget,
        DateOnly StartDate, DateOnly? EndDate, int TaskCount, DateTimeOffset CreatedAt);

    public record ProjectDetailDto(Guid Id, string Code, string Name,
        Guid CustomerId, string CustomerName,
        Guid? SalesOrderId, string? SalesOrderNo,
        Guid? ProjectManagerId, string? ProjectManagerName,
        string Status, int Progress, decimal Budget,
        DateOnly StartDate, DateOnly? EndDate, string? Notes,
        DateTimeOffset CreatedAt, List<TaskDto> Tasks,
        string RevenueRecognitionMethod, decimal? EstimatedTotalCost,
        decimal UnbilledRevenueBalance, decimal OverbilledBalance);

    public record TaskDto(Guid Id, string Title, string? Description,
        Guid? AssignedToId, string? AssignedToName,
        string Status, string Priority, DateOnly? DueDate, int SortOrder);

    public record CreateProjectRequest(
        string Name, Guid CustomerId, Guid? SalesOrderId,
        Guid? ProjectManagerId, DateOnly StartDate, DateOnly? EndDate,
        decimal Budget, string? Notes,
        string? RevenueRecognitionMethod = null, decimal? EstimatedTotalCost = null);

    public record UpdateProjectRequest(
        string Name, Guid CustomerId, Guid? SalesOrderId,
        Guid? ProjectManagerId, DateOnly StartDate, DateOnly? EndDate,
        decimal Budget, int Progress, string Status, string? Notes,
        string? RevenueRecognitionMethod = null, decimal? EstimatedTotalCost = null,
        bool ConfirmRevenueTrueUp = false);

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

    public record RevenueRecognitionResultDto(
        decimal PercentageComplete,
        decimal IncrementalRevenue,
        decimal CumulativeRevenueRecognized,
        decimal ActualCostToDate);

    public record ProjectRevenueRecognitionDto(
        Guid Id, DateTimeOffset RecognitionDate, decimal ActualCostToDate,
        decimal PercentageComplete, decimal CumulativeRevenueRecognized,
        decimal IncrementalRevenueThisEntry, Guid? JournalEntryId, string? JournalEntryNo);

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
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ProjectListDto>>>> List([FromQuery] ProjectQueryParams p)
    {
        var q = db.Projects
            .AsNoTracking()
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

        if (!string.IsNullOrWhiteSpace(p.Status) &&
            Enum.TryParse<ProjectStatus>(p.Status, true, out var statusFilter))
        {
            q = q.Where(x => x.Status == statusFilter);
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
            .AsNoTracking()
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
                t.Status.ToString(), t.Priority.ToString(), t.DueDate, t.SortOrder)).ToList(),
            p.RevenueRecognitionMethod.ToString(), p.EstimatedTotalCost,
            p.UnbilledRevenueBalance, p.OverbilledBalance);

        return Ok(ApiResponse<ProjectDetailDto>.Ok(dto));
    }

    [HttpPost]
    public Task<ActionResult<ApiResponse<ProjectListDto>>> Create([FromBody] CreateProjectRequest req) =>
        SequentialCodeHelper.RunWithRetryAsync(db, () => CreateCoreAsync(req));

    private async Task<ActionResult<ApiResponse<ProjectListDto>>> CreateCoreAsync(CreateProjectRequest req)
    {
        var method = ParseRevenueRecognitionMethod(req.RevenueRecognitionMethod);
        var validationError = await ValidateRevenueRecognitionAndSalesOrderAsync(
            req.SalesOrderId, excludeProjectId: null, method, req.EstimatedTotalCost);
        if (validationError is not null)
            throw new InvalidOperationException(validationError);

        var project = new Project
        {
            Code             = await GenerateCodeAsync(),
            Name             = req.Name,
            CustomerId       = req.CustomerId,
            SalesOrderId     = req.SalesOrderId,
            ProjectManagerId = req.ProjectManagerId,
            StartDate        = req.StartDate,
            EndDate          = req.EndDate,
            Budget           = req.Budget,
            Notes            = req.Notes,
            Status           = ProjectStatus.Planning,
            RevenueRecognitionMethod = method,
            EstimatedTotalCost       = req.EstimatedTotalCost,
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

        var method = ParseRevenueRecognitionMethod(req.RevenueRecognitionMethod);
        var validationError = await ValidateRevenueRecognitionAndSalesOrderAsync(
            req.SalesOrderId, excludeProjectId: id, method, req.EstimatedTotalCost);
        if (validationError is not null)
            throw new InvalidOperationException(validationError);

        var isCompletingNow = status == ProjectStatus.Completed && project.Status != ProjectStatus.Completed;

        // Project tidak boleh Completed tanpa Invoice aktif sama sekali - berlaku untuk KEDUA
        // RevenueRecognitionMethod (Immediate maupun PercentageOfCompletion), TERPISAH dari true-up
        // di bawah. True-up cuma menghitung sisa pendapatan yang belum diakui dari SalesOrder.Total,
        // sama sekali tidak menyentuh keberadaan Invoice - jadi Project ber-method Immediate (semua
        // 15 Project live saat ini pakai method ini) tidak pernah tersentuh true-up sama sekali dan
        // butuh pengecekan sendiri di sini. Project tanpa SalesOrderId otomatis gagal (Invoice selalu
        // terhubung lewat SalesOrderId, jadi tidak mungkin ada Invoice tanpa SO).
        if (isCompletingNow)
        {
            var hasActiveInvoice = project.SalesOrderId.HasValue &&
                await db.Invoices.AnyAsync(i => i.SalesOrderId == project.SalesOrderId.Value && !i.IsDeleted);
            if (!hasActiveInvoice)
                throw new InvalidOperationException(
                    "Project tidak bisa diselesaikan karena belum ada Invoice aktif untuk Sales Order terkait.");
        }

        // True-up (Fase B3): kalau Project POC ditutup (→Completed) sebelum % completion mencapai
        // 100 dari sisi Catat Progres manual, sisa pendapatan yang belum diakui WAJIB diakui sekaligus
        // di titik penutupan - kalau tidak, sisanya hilang permanen (tidak ada lagi kesempatan Catat
        // Progres untuk Project yang sudah Completed). Butuh konfirmasi eksplisit dari user dulu
        // (ConfirmRevenueTrueUp) karena ini mem-posting jurnal - request pertama tanpa konfirmasi
        // HARUS murni informatif (400/409), TIDAK BOLEH mengubah project/db apa pun.
        if (isCompletingNow && project.RevenueRecognitionMethod == RevenueRecognitionMethod.PercentageOfCompletion)
        {
            var contractValue = await ComputeContractValueAsync(project.SalesOrderId);
            var lastRecognition = await db.ProjectRevenueRecognitions
                .Where(r => r.ProjectId == project.Id)
                .OrderByDescending(r => r.RecognitionDate)
                .FirstOrDefaultAsync();
            var cumulativeSoFar = lastRecognition?.CumulativeRevenueRecognized ?? 0;
            var remainingRevenue = MoneyMath.Round(contractValue - cumulativeSoFar);

            // Dead-band 0.01m ini peninggalan era rounding 2-desimal (sen) - sejak MoneyMath.Round
            // membulatkan semua nilai uang ke 0 desimal, residual terkecil yang mungkin adalah
            // Rp1, jadi threshold ini tetap benar (1 > 0.01) tapi granularitasnya sekarang whole-Rupiah.
            if (remainingRevenue > 0.01m)
            {
                if (!req.ConfirmRevenueTrueUp)
                {
                    // ApiResponse<T> sudah punya Data yang tetap bisa dipakai walau Success=false
                    // (lihat ApiResponse<T>.Fail() cuma helper, propertinya sendiri tidak dibatasi) -
                    // jadi tidak perlu shape response baru, cukup construct manual di sini.
                    return Conflict(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Menutup proyek ini akan mengakui sisa pendapatan Rp {remainingRevenue:N0}. Konfirmasi untuk lanjutkan.",
                        Data = new
                        {
                            requiresConfirmation = true,
                            remainingRevenue,
                            contractValue,
                            currentPercentage = contractValue > 0 ? Math.Round(cumulativeSoFar / contractValue * 100, 2) : 0,
                        },
                    });
                }

                await using var tx = await db.Database.BeginTransactionAsync();

                var actualCostFresh = await ComputeActualCostToDateAsync(project.SalesOrderId);
                var prr = new ProjectRevenueRecognition
                {
                    ProjectId                   = project.Id,
                    RecognitionDate             = DateTimeOffset.UtcNow,
                    ActualCostToDate            = actualCostFresh,
                    PercentageComplete          = 100,
                    CumulativeRevenueRecognized = contractValue,
                    IncrementalRevenueThisEntry = remainingRevenue,
                };
                db.ProjectRevenueRecognitions.Add(prr);
                await db.SaveChangesAsync();

                var journalId = await journalPostingService.PostAsync(
                    $"Pengakuan Pendapatan Penutupan Proyek {project.Code} (true-up ke 100%)",
                    JournalSourceType.RevenueRecognition,
                    prr.Id,
                    DateTimeOffset.UtcNow,
                    new PostingLine[]
                    {
                        new("1-2200", remainingRevenue, 0, "Piutang Belum Ditagih"),
                        new("4-1000", 0, remainingRevenue, "Pendapatan Penjualan"),
                    });

                prr.JournalEntryId = journalId;
                project.UnbilledRevenueBalance += remainingRevenue;
                await db.SaveChangesAsync();

                await tx.CommitAsync();
            }
        }

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
        project.RevenueRecognitionMethod = method;
        project.EstimatedTotalCost       = req.EstimatedTotalCost;
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
            .AsNoTracking()
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

    // ── Revenue Recognition (Percentage of Completion) ──────────────────────────

    [HttpPost("{id:guid}/revenue-recognition")]
    public async Task<ActionResult<ApiResponse<RevenueRecognitionResultDto>>> RecordRevenueRecognition(Guid id)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == id);
        if (project is null)
            return NotFound(ApiResponse<RevenueRecognitionResultDto>.Fail("Project tidak ditemukan."));

        // Re-check di sini juga (bukan cuma di Create/Update) - data yang sudah ada sebelum validasi
        // di poin 3 ditambahkan bisa saja belum konsisten.
        if (project.RevenueRecognitionMethod != RevenueRecognitionMethod.PercentageOfCompletion)
            throw new InvalidOperationException("Project ini tidak memakai metode Percentage of Completion.");
        if (project.EstimatedTotalCost is null || project.EstimatedTotalCost <= 0)
            throw new InvalidOperationException("Project ini belum punya Estimated Total Cost yang valid.");
        if (project.SalesOrderId is null)
            throw new InvalidOperationException("Project ini tidak terhubung ke Sales Order.");

        var actualCostToDate = await ComputeActualCostToDateAsync(project.SalesOrderId);
        var percentageComplete = Math.Min(100m,
            Math.Round(actualCostToDate / project.EstimatedTotalCost.Value * 100, 2));

        var contractValue = await ComputeContractValueAsync(project.SalesOrderId);
        var cumulativeNew = MoneyMath.Round(contractValue * percentageComplete / 100);

        var prevEntry = await db.ProjectRevenueRecognitions
            .Where(x => x.ProjectId == id)
            .OrderByDescending(x => x.RecognitionDate)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        var cumulativePrev = prevEntry?.CumulativeRevenueRecognized ?? 0;
        var incrementalRevenue = cumulativeNew - cumulativePrev;

        if (incrementalRevenue <= 0)
            throw new InvalidOperationException(
                "Tidak ada progres baru untuk diakui (% completion tidak bertambah sejak pencatatan terakhir).");

        var entry = new ProjectRevenueRecognition
        {
            ProjectId                    = id,
            RecognitionDate              = DateTimeOffset.UtcNow,
            ActualCostToDate             = actualCostToDate,
            PercentageComplete           = percentageComplete,
            CumulativeRevenueRecognized  = cumulativeNew,
            IncrementalRevenueThisEntry  = incrementalRevenue,
        };
        db.ProjectRevenueRecognitions.Add(entry);
        await db.SaveChangesAsync();

        var journalEntryId = await journalPostingService.PostAsync(
            $"Pengakuan Pendapatan Proyek {project.Code} ({percentageComplete:0.00}%)",
            JournalSourceType.RevenueRecognition,
            entry.Id,
            DateTimeOffset.UtcNow,
            new PostingLine[]
            {
                new("1-2200", incrementalRevenue, 0, "Piutang Belum Ditagih"),
                new("4-1000", 0, incrementalRevenue, "Pendapatan Penjualan"),
            });

        entry.JournalEntryId = journalEntryId;
        project.UnbilledRevenueBalance += incrementalRevenue;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        await tx.CommitAsync();

        return Ok(ApiResponse<RevenueRecognitionResultDto>.Ok(new RevenueRecognitionResultDto(
            PercentageComplete:          percentageComplete,
            IncrementalRevenue:          incrementalRevenue,
            CumulativeRevenueRecognized: cumulativeNew,
            ActualCostToDate:            actualCostToDate),
            "Progres pendapatan berhasil dicatat."));
    }

    [HttpGet("{id:guid}/revenue-recognition")]
    public async Task<ActionResult<ApiResponse<List<ProjectRevenueRecognitionDto>>>> ListRevenueRecognition(Guid id)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == id))
            return NotFound(ApiResponse<List<ProjectRevenueRecognitionDto>>.Fail("Project tidak ditemukan."));

        var rows = await db.ProjectRevenueRecognitions
            .AsNoTracking()
            .Where(x => x.ProjectId == id)
            .OrderBy(x => x.RecognitionDate)
            .ToListAsync();

        var journalEntryIds = rows.Where(x => x.JournalEntryId.HasValue)
            .Select(x => x.JournalEntryId!.Value).ToList();
        var journalNumbers = await db.JournalEntries
            .Where(j => journalEntryIds.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, j => j.EntryNumber);

        var result = rows.Select(x => new ProjectRevenueRecognitionDto(
            x.Id, x.RecognitionDate, x.ActualCostToDate, x.PercentageComplete,
            x.CumulativeRevenueRecognized, x.IncrementalRevenueThisEntry, x.JournalEntryId,
            x.JournalEntryId.HasValue && journalNumbers.TryGetValue(x.JournalEntryId.Value, out var no)
                ? no : null)).ToList();

        return Ok(ApiResponse<List<ProjectRevenueRecognitionDto>>.Ok(result));
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

    private Task<string> GenerateCodeAsync() =>
        SequentialCodeHelper.NextYearCodeAsync(db.Projects, "PRJ", 3, DateTime.UtcNow.Year);

    private static RevenueRecognitionMethod ParseRevenueRecognitionMethod(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return RevenueRecognitionMethod.Immediate;
        if (!Enum.TryParse<RevenueRecognitionMethod>(raw, true, out var method))
            throw new InvalidOperationException($"Revenue Recognition Method '{raw}' tidak valid.");
        return method;
    }

    // Dipakai di Create dan Update - hardening loophole yang ditemukan investigasi (POST /api/projects
    // dan PUT /api/projects/{id} sebelumnya bisa memasang SalesOrderId yang sama ke lebih dari satu
    // Project tanpa ditolak). Project Cancelled/Completed dianggap tidak lagi "menyandera" SO-nya.
    private async Task<string?> ValidateRevenueRecognitionAndSalesOrderAsync(
        Guid? salesOrderId, Guid? excludeProjectId,
        RevenueRecognitionMethod method, decimal? estimatedTotalCost)
    {
        if (method == RevenueRecognitionMethod.PercentageOfCompletion)
        {
            if (salesOrderId is null)
                return "Metode Percentage of Completion butuh Project terhubung ke Sales Order.";
            if (estimatedTotalCost is null || estimatedTotalCost <= 0)
                return "Metode Percentage of Completion butuh Estimated Total Cost yang valid.";
        }

        if (salesOrderId is not null)
        {
            var activeStatuses = new[] { ProjectStatus.Planning, ProjectStatus.Running, ProjectStatus.OnHold };
            var conflict = await db.Projects
                .Where(p => p.SalesOrderId == salesOrderId.Value
                         && activeStatuses.Contains(p.Status)
                         && (!excludeProjectId.HasValue || p.Id != excludeProjectId.Value))
                .Select(p => new { p.Code, p.Name })
                .FirstOrDefaultAsync();
            if (conflict is not null)
                return $"Sales Order ini sudah dipakai Project lain: {conflict.Code} - {conflict.Name}.";
        }

        return null;
    }

    // ActualCostToDate ("ReceivedCost") - pola JOIN sama persis dengan VendorPayment di GetCost
    // (PurchaseRequest → PurchaseOrder), targetnya diganti ke PurchaseOrderItem.ReceivedQty*Price.
    // Dipakai baik oleh RecordRevenueRecognition maupun true-up di Update (Fase B3).
    private async Task<decimal> ComputeActualCostToDateAsync(Guid? salesOrderId)
    {
        if (salesOrderId is null) return 0;

        var prIds = await db.PurchaseRequests
            .Where(pr => pr.SalesOrderId == salesOrderId.Value)
            .Select(pr => pr.Id)
            .ToListAsync();

        if (prIds.Count == 0) return 0;

        var poIds = await db.PurchaseOrders
            .Where(po => po.PurchaseRequestId.HasValue && prIds.Contains(po.PurchaseRequestId.Value))
            .Select(po => po.Id)
            .ToListAsync();

        if (poIds.Count == 0) return 0;

        return await db.PurchaseOrderItems
            .Where(item => poIds.Contains(item.POId))
            .SumAsync(item => (decimal?)(item.ReceivedQty * item.Price)) ?? 0;
    }

    // ContractValue = SUM(SalesOrderItem.Amount), pre-tax - BUKAN SalesOrder.Total (yang include PPN).
    private async Task<decimal> ComputeContractValueAsync(Guid? salesOrderId)
    {
        if (salesOrderId is null) return 0;

        return await db.SalesOrderItems
            .Where(i => i.SalesOrderId == salesOrderId.Value)
            .SumAsync(i => (decimal?)i.Amount) ?? 0;
    }
}
