using System.Net;
using System.Text;

namespace Crm.Services.Wiki
{
    /// <summary>Фрагменты текста для результатов поиска с подсветкой совпадений (безопасный HTML).</summary>
    public static class WikiSnippet
    {
        /// <summary>HTML-кодирует текст и оборачивает все вхождения запроса (без учёта регистра) в &lt;mark&gt;.</summary>
        public static string Highlight(string? text, string? query)
        {
            text ??= string.Empty;
            if (string.IsNullOrWhiteSpace(query))
                return WebUtility.HtmlEncode(text);

            query = query.Trim();
            var sb = new StringBuilder();
            var position = 0;
            while (position < text.Length)
            {
                var index = text.IndexOf(query, position, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    break;
                sb.Append(WebUtility.HtmlEncode(text[position..index]));
                sb.Append("<mark>").Append(WebUtility.HtmlEncode(text.Substring(index, query.Length))).Append("</mark>");
                position = index + query.Length;
            }
            sb.Append(WebUtility.HtmlEncode(text[position..]));
            return sb.ToString();
        }

        /// <summary>Отрывок текста вокруг первого совпадения (или начало текста), с подсветкой.</summary>
        public static string Excerpt(string? text, string? query, int radius = 90)
        {
            text = (text ?? string.Empty).Replace('\n', ' ');
            if (text.Length == 0)
                return string.Empty;

            var index = string.IsNullOrWhiteSpace(query) ? -1 : text.IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                var head = text.Length > radius * 2 ? text[..(radius * 2)].TrimEnd() + "…" : text;
                return Highlight(head, query);
            }

            var start = Math.Max(0, index - radius);
            var end = Math.Min(text.Length, index + query!.Trim().Length + radius);
            // Не режем слова пополам
            if (start > 0)
            {
                var space = text.IndexOf(' ', start);
                if (space > 0 && space < index)
                    start = space + 1;
            }
            if (end < text.Length)
            {
                var space = text.LastIndexOf(' ', end);
                if (space > index + query.Trim().Length)
                    end = space;
            }

            var fragment = text[start..end];
            return (start > 0 ? "…" : "") + Highlight(fragment, query) + (end < text.Length ? "…" : "");
        }
    }
}
