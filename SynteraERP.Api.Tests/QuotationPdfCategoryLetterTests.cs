using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services;

namespace SynteraERP.Api.Tests;

// Task #45: huruf kategori (A, B, C, ...) computed-on-render dari urutan Tab->Group, dipakai
// bersama oleh RenderSummaryContent dan RenderWorkItemsContent lewat BuildGroupCategoryLetters.
// Pure logic, tidak butuh DB — QuotationPdfService.BuildGroupCategoryLetters/ToCategoryLetter
// diubah dari private ke internal + InternalsVisibleTo (SynteraERP.Api.csproj) supaya bisa
// ditest langsung, karena tidak ada library ekstraksi teks PDF di test project dan tidak ada
// pola test PDF yang sudah ada untuk diikuti (dicek sebelum nulis test ini).
public class QuotationPdfCategoryLetterTests
{
    [Theory]
    [InlineData(0, "A")]
    [InlineData(1, "B")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(27, "AB")]
    [InlineData(51, "AZ")]
    [InlineData(52, "BA")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    public void ToCategoryLetter_matches_Excel_style_base26_labeling(int index, string expected)
    {
        QuotationPdfService.ToCategoryLetter(index).Should().Be(expected);
    }

    private static QuotationGroup MakeGroup(int sortOrder, bool hasContent) => new()
    {
        TabId = Guid.NewGuid(),
        Name = $"Group {sortOrder}",
        SortOrder = sortOrder,
        WorkItems = hasContent
            ? [new QuotationWorkItem { Name = "W", SortOrder = 0 }]
            : [],
    };

    [Fact]
    public void BuildGroupCategoryLetters_assigns_sequentially_across_multiple_tabs()
    {
        var tab1 = new QuotationTab
        {
            SortOrder = 0,
            Groups = [MakeGroup(0, true), MakeGroup(1, true)],
        };
        var tab2 = new QuotationTab
        {
            SortOrder = 1,
            Groups = [MakeGroup(0, true)],
        };
        var quotation = new Quotation { Tabs = [tab2, tab1] }; // sengaja dibalik — urutan harus ikut Tab.SortOrder, bukan urutan koleksi

        var letters = QuotationPdfService.BuildGroupCategoryLetters(quotation);

        letters[tab1.Groups.ElementAt(0).Id].Should().Be("A");
        letters[tab1.Groups.ElementAt(1).Id].Should().Be("B");
        letters[tab2.Groups.ElementAt(0).Id].Should().Be("C");
    }

    // Ini skenario risiko utama yang diminta diverifikasi: RenderWorkItemsContent memfilter Group
    // tanpa WorkItems/Items dari list-nya sendiri (lihat `groups` di method itu). Kalau huruf
    // dihitung ulang dari list yang SUDAH difilter itu, Group setelah satu Group kosong akan
    // dapat huruf yang salah (bergeser). BuildGroupCategoryLetters harus dipanggil SEKALI dari
    // daftar Group lengkap dan di-lookup by Id — bukan dihitung ulang per halaman.
    [Fact]
    public void Group_letter_stays_correct_when_an_earlier_group_would_be_filtered_out_of_BOQ()
    {
        var emptyGroup = MakeGroup(0, hasContent: false);   // BOQ page would skip this one
        var groupB = MakeGroup(1, hasContent: true);
        var groupC = MakeGroup(2, hasContent: true);
        var tab = new QuotationTab { SortOrder = 0, Groups = [emptyGroup, groupB, groupC] };
        var quotation = new Quotation { Tabs = [tab] };

        var letters = QuotationPdfService.BuildGroupCategoryLetters(quotation);

        // Full (Recapitulation) order: A=empty, B=groupB, C=groupC.
        letters[emptyGroup.Id].Should().Be("A");
        letters[groupB.Id].Should().Be("B");
        letters[groupC.Id].Should().Be("C");

        // Simulate RenderWorkItemsContent's own filtered list (same predicate as production code)
        // and confirm looking up from the SAME dictionary — not recomputing from this shorter
        // list — keeps groupB="B" and groupC="C", not "A"/"B" as a naive re-index would produce.
        var boqGroups = tab.Groups.Where(g => g.WorkItems.Count > 0 || g.Items.Count > 0).ToList();
        boqGroups.Should().HaveCount(2);
        letters[boqGroups[0].Id].Should().Be("B");
        letters[boqGroups[1].Id].Should().Be("C");
    }
}
