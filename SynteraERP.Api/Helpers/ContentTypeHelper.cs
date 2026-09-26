namespace SynteraERP.Api.Helpers;

// Backend duplicate-logic audit (26 Sep 2026): CustomerPoService and ExpenseService had a
// byte-identical GetContentType switch (pdf/jpg/jpeg/png/xlsx/docx); CompanySettingsService had a
// 3rd copy that the original audit missed, scoped narrower to logo images
// (png/jpg/jpeg/gif/webp, no pdf/xlsx/docx). Extracted here as a union of both extension sets so
// all three attachment/logo download paths share one implementation.
public static class ContentTypeHelper
{
    public static string FromPath(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream",
        };
}
