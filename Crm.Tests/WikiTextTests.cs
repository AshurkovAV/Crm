using Crm.Services.Wiki;
using Xunit;

namespace Crm.Tests;

public class WikiTextTests
{
    [Fact]
    public void Diff_marks_added_and_removed_lines()
    {
        var diff = WikiDiff.Compare("a\nb\nc", "a\nB\nc\nd");
        Assert.Equal(new[]
        {
            (WikiDiffKind.Same, "a"), (WikiDiffKind.Removed, "b"), (WikiDiffKind.Added, "B"),
            (WikiDiffKind.Same, "c"), (WikiDiffKind.Added, "d"),
        }, diff.Select(d => (d.Kind, d.Text)));
    }

    [Fact]
    public void Diff_of_identical_text_has_no_changes()
        => Assert.All(WikiDiff.Compare("x\ny", "x\ny"), d => Assert.Equal(WikiDiffKind.Same, d.Kind));

    [Fact]
    public void Highlight_encodes_html_and_marks_matches()
    {
        var html = WikiSnippet.Highlight("<b>Регламент</b> регламента", "регламент");
        Assert.Equal("&lt;b&gt;<mark>Регламент</mark>&lt;/b&gt; <mark>регламент</mark>а", html);
    }

    [Fact]
    public void Query_with_markup_cannot_inject_html()
        => Assert.DoesNotContain("<script", WikiSnippet.Excerpt("text <script> text", "<script>"));

    [Theory]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("C:\\temp\\отчёт.pdf", "отчёт.pdf")]
    [InlineData("a<b>.txt", "a_b_.txt")]
    public void File_names_are_cleaned(string input, string expected)
        => Assert.Equal(expected, WikiFileStorage.CleanFileName(input));

    [Theory]
    [InlineData("page.html", "application/octet-stream")]
    [InlineData("logo.svg", "application/octet-stream")]
    [InlineData("doc.pdf", "application/pdf")]
    [InlineData("photo.JPG", "image/jpeg")]
    public void Content_type_never_allows_active_content(string name, string expected)
        => Assert.Equal(expected, WikiFileStorage.ResolveContentType(name));

    [Fact]
    public void Inline_preview_requires_real_signature()
    {
        Assert.False(WikiFileStorage.CanPreviewInline("image/png", "<svg onload=1>"u8));
        Assert.True(WikiFileStorage.CanPreviewInline("application/pdf", "%PDF-1.7"u8));
        Assert.False(WikiFileStorage.CanPreviewInline("application/pdf", "<html>"u8));
    }

    [Fact]
    public void Templates_survive_sanitizer()
    {
        foreach (var t in WikiTemplates.All.Where(t => t.Html.Length > 0))
            Assert.Equal(WikiHtml.ToPlainText(t.Html), WikiHtml.ToPlainText(WikiHtml.Sanitize(t.Html)));
    }
}
