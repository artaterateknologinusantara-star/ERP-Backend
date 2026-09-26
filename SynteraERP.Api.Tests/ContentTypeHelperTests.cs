using FluentAssertions;
using Xunit;
using SynteraERP.Api.Helpers;

namespace SynteraERP.Api.Tests;

// Backend duplicate-logic audit (26 Sep 2026): CustomerPoService and ExpenseService had a
// byte-identical GetContentType switch; CompanySettingsService had a 3rd, narrower copy (missed by
// the original audit) for logo images only. Extracted as a union of both extension sets - these
// tests pin down every extension each of the 3 original call sites relied on, so future edits
// can't silently drop one attachment/logo type.
public class ContentTypeHelperTests
{
    [Theory]
    [InlineData("file.pdf", "application/pdf")]
    [InlineData("file.jpg", "image/jpeg")]
    [InlineData("file.jpeg", "image/jpeg")]
    [InlineData("file.png", "image/png")]
    [InlineData("file.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("file.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void FromPath_resolves_attachment_extensions_used_by_CustomerPoService_and_ExpenseService(string path, string expected)
    {
        ContentTypeHelper.FromPath(path).Should().Be(expected);
    }

    [Theory]
    [InlineData("logo.gif", "image/gif")]
    [InlineData("logo.webp", "image/webp")]
    public void FromPath_resolves_logo_only_extensions_used_by_CompanySettingsService(string path, string expected)
    {
        ContentTypeHelper.FromPath(path).Should().Be(expected);
    }

    [Theory]
    [InlineData("file.PDF")]
    [InlineData("file.PNG")]
    [InlineData("file.WEBP")]
    public void FromPath_is_case_insensitive(string path)
    {
        ContentTypeHelper.FromPath(path).Should().NotBe("application/octet-stream");
    }

    [Fact]
    public void FromPath_falls_back_to_octet_stream_for_unknown_extensions()
    {
        ContentTypeHelper.FromPath("file.exe").Should().Be("application/octet-stream");
    }
}
