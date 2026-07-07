namespace SynteraERP.Api.DTOs.SystemReset;

public record ResetResultDto(
    bool Success,
    string Message,
    Dictionary<string, int> DeletedCounts,
    Guid AuditLogId
);
