using Crm.Services.Wiki;
using Xunit;

namespace Crm.Tests;

public class WikiHtmlTests
{
    [Theory]
    [InlineData("<p>ok<script>alert(1)</script></p>", "<script")]
    [InlineData("<img src=x onerror=alert(1)>", "onerror")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "javascript:")]
    [InlineData("<p style=\"background:url(x)\">x</p>", "style")]
    [InlineData("<iframe src=\"https://evil.example/embed/x\"></iframe>", "evil.example")]
    [InlineData("<iframe srcdoc=\"<script>alert(1)</script>\"></iframe>", "srcdoc")]
    [InlineData("<svg><script>alert(1)</script></svg>", "<svg")]
    [InlineData("<img src=\"https://tracker.example/p.gif\">", "tracker.example")]
    [InlineData("<img src=\"data:image/png;base64,AAAA\">", "data:")]
    [InlineData("<a href=\"//evil.example\">x</a>", "evil.example")]
    [InlineData("<input type=\"text\" value=\"x\">", "type=\"text\"")]
    [InlineData("<form action=\"/x\"><button>x</button></form>", "<form")]
    [InlineData("<div onclick=\"alert(1)\" data-callout=\"info\">x</div>", "onclick")]
    public void Dangerous_markup_is_removed(string html, string forbidden)
    {
        var clean = WikiHtml.Sanitize(html);
        Assert.DoesNotContain(forbidden, clean, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Editor_markup_is_preserved()
    {
        var html = "<h2>Заголовок</h2><p><strong>b</strong> <em>i</em> <u>u</u> <s>s</s> <code>c</code></p>"
                   + "<div data-callout=\"warning\"><p>Внимание</p></div>"
                   + "<ul data-type=\"taskList\"><li data-checked=\"true\" data-type=\"taskItem\"><label><input type=\"checkbox\" checked><span></span></label><div><p>Пункт</p></div></li></ul>"
                   + "<table><tbody><tr><th colspan=\"1\" rowspan=\"1\"><p>A</p></th></tr></tbody></table>"
                   + "<img src=\"/Wiki/File/12\" alt=\"схема\">"
                   + "<a href=\"/Wiki/Page/5\" data-page-id=\"5\">ссылка</a>"
                   + "<div data-video=\"\"><iframe src=\"https://www.youtube.com/embed/dQw4w9WgXcQ\"></iframe></div>";
        var clean = WikiHtml.Sanitize(html);
        Assert.Contains("<h2>Заголовок</h2>", clean);
        Assert.Contains("data-callout=\"warning\"", clean);
        Assert.Contains("data-type=\"taskItem\"", clean);
        Assert.Contains("type=\"checkbox\"", clean);
        Assert.Contains("<table>", clean);
        Assert.Contains("src=\"/Wiki/File/12\"", clean);
        Assert.Contains("data-page-id=\"5\"", clean);
        Assert.Contains("src=\"https://www.youtube.com/embed/dQw4w9WgXcQ\"", clean);
        Assert.Contains("sandbox=", clean);
    }

    [Theory]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", true)]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ?start=42", true)]
    [InlineData("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", true)]
    [InlineData("https://rutube.ru/play/embed/abcdef1234567890abcdef1234567890", true)]
    [InlineData("https://vk.com/video_ext.php?oid=-123&id=456&hash=abc", true)]
    [InlineData("https://vkvideo.ru/video_ext.php?oid=1&id=2", true)]
    [InlineData("http://www.youtube.com/embed/dQw4w9WgXcQ", false)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", false)]
    [InlineData("https://www.youtube.com.evil.example/embed/dQw4w9WgXcQ", false)]
    [InlineData("https://vk.com/video_ext.php?oid=1&id=2&x=<script>", false)]
    public void Video_whitelist(string url, bool allowed) => Assert.Equal(allowed, WikiHtml.IsAllowedVideoUrl(url));

    [Fact]
    public void Plain_text_has_lines_and_checklist_marks()
    {
        var text = WikiHtml.ToPlainText("<h2>Шаги</h2><ul data-type=\"taskList\"><li data-type=\"taskItem\" data-checked=\"true\"><div><p>Готово</p></div></li><li data-type=\"taskItem\" data-checked=\"false\"><div><p>Не готово</p></div></li></ul><p>Конец</p>");
        Assert.Equal("Шаги\n[x] Готово\n[ ] Не готово\nКонец", text);
    }

    [Fact]
    public void Table_row_is_one_line()
        => Assert.Equal("A | B\n1 | 2", WikiHtml.ToPlainText("<table><tbody><tr><th><p>A</p></th><th><p>B</p></th></tr><tr><td><p>1</p></td><td><p>2</p></td></tr></tbody></table>"));

    [Fact]
    public void Checklist_is_extracted_for_tasks()
    {
        var items = WikiHtml.ExtractChecklist("<ul data-type=\"taskList\"><li data-type=\"taskItem\" data-checked=\"true\"><div><p>A</p></div></li><li data-type=\"taskItem\" data-checked=\"false\"><div><p>B</p></div></li></ul>");
        Assert.Equal(new[] { ("A", true), ("B", false) }, items);
    }
}
