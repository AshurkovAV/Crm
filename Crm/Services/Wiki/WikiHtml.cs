using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace Crm.Services.Wiki
{
    /// <summary>
    /// Очистка HTML страниц базы знаний и извлечение из него текста.
    /// Читателям выводится ТОЛЬКО результат Sanitize: белый список тегов/атрибутов редактора,
    /// картинки — только наши файлы (/Wiki/File/{id}), iframe — только плееры YouTube / Rutube / VK Видео.
    /// </summary>
    public static class WikiHtml
    {
        public const int MaxHtmlLength = 2_000_000;

        private static readonly Regex InternalImage = new(@"^/Wiki/File/\d{1,10}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Разрешённые адреса встраиваемых плееров (только https и только страница-плеер).</summary>
        private static readonly Regex[] VideoEmbeds =
        {
            new(@"^https://www\.youtube(-nocookie)?\.com/embed/[A-Za-z0-9_-]{6,20}(\?start=\d{1,6})?$", RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new(@"^https://rutube\.ru/play/embed/[A-Za-z0-9]{6,40}/?(\?p=[A-Za-z0-9_-]{1,64})?$", RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new(@"^https://(vk\.com|vkvideo\.ru)/video_ext\.php\?oid=-?\d{1,20}&id=\d{1,20}(&hash=[A-Za-z0-9]{1,40})?(&hd=\d)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        };

        private static readonly string[] AllowedTags =
        {
            "p", "br", "h1", "h2", "h3", "h4", "strong", "b", "em", "i", "u", "s", "strike", "code", "pre",
            "blockquote", "ul", "ol", "li", "hr", "a", "img", "table", "thead", "tbody", "tr", "th", "td",
            "colgroup", "col", "div", "span", "label", "input", "iframe", "mark", "sub", "sup",
        };

        private static readonly string[] AllowedAttributes =
        {
            "href", "target", "rel", "src", "alt", "title", "colspan", "rowspan", "colwidth", "start",
            "type", "checked", "disabled",
            "data-type", "data-checked", "data-callout", "data-video", "data-page-id", "data-attachment-id", "data-language",
        };

        private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

        public static bool IsAllowedVideoUrl(string? url)
            => !string.IsNullOrEmpty(url) && VideoEmbeds.Any(r => r.IsMatch(url));

        public static bool IsAllowedImageUrl(string? url)
            => !string.IsNullOrEmpty(url) && InternalImage.IsMatch(url);

        public static string Sanitize(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;
            if (html.Length > MaxHtmlLength)
                html = html[..MaxHtmlLength];
            return Sanitizer.Sanitize(html);
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Clear();
            foreach (var tag in AllowedTags)
                sanitizer.AllowedTags.Add(tag);

            sanitizer.AllowedAttributes.Clear();
            foreach (var attribute in AllowedAttributes)
                sanitizer.AllowedAttributes.Add(attribute);

            sanitizer.AllowedCssProperties.Clear();
            sanitizer.AllowedAtRules.Clear();
            sanitizer.AllowDataAttributes = false;

            sanitizer.AllowedSchemes.Clear();
            foreach (var scheme in new[] { "http", "https", "mailto", "tel" })
                sanitizer.AllowedSchemes.Add(scheme);

            sanitizer.FilterUrl += (_, args) =>
            {
                var tag = args.Tag.TagName.ToLowerInvariant();
                var url = args.OriginalUrl?.Trim() ?? string.Empty;
                if (tag == "img")
                {
                    args.SanitizedUrl = IsAllowedImageUrl(url) ? url : null;
                }
                else if (tag == "iframe")
                {
                    args.SanitizedUrl = IsAllowedVideoUrl(url) ? url : null;
                }
                else if (url.StartsWith("//", StringComparison.Ordinal))
                {
                    // Протокол-относительные ссылки ведут на чужой хост — запрещаем
                    args.SanitizedUrl = null;
                }
            };

            // Доводка уже очищенного документа: обязательные атрибуты iframe/img/ссылок,
            // удаление элементов, оставшихся без допустимого адреса.
            sanitizer.PostProcessDom += (_, args) =>
            {
                var root = args.Document.Body;
                if (root == null)
                    return;

                foreach (var element in root.QuerySelectorAll("iframe").ToList())
                {
                    if (!IsAllowedVideoUrl(element.GetAttribute("src")))
                    {
                        element.Remove();
                        continue;
                    }
                    element.SetAttribute("sandbox", "allow-scripts allow-same-origin allow-presentation allow-popups");
                    element.SetAttribute("allow", "fullscreen; picture-in-picture; encrypted-media");
                    element.SetAttribute("allowfullscreen", "");
                    element.SetAttribute("loading", "lazy");
                    element.SetAttribute("referrerpolicy", "strict-origin-when-cross-origin");
                    element.SetAttribute("frameborder", "0");
                }

                foreach (var element in root.QuerySelectorAll("img").ToList())
                {
                    if (!IsAllowedImageUrl(element.GetAttribute("src")))
                    {
                        element.Remove();
                        continue;
                    }
                    element.SetAttribute("loading", "lazy");
                }

                foreach (var element in root.QuerySelectorAll("input").ToList())
                {
                    if (!string.Equals(element.GetAttribute("type"), "checkbox", StringComparison.OrdinalIgnoreCase))
                    {
                        element.Remove();
                        continue;
                    }
                    element.SetAttribute("disabled", "disabled");
                }

                foreach (var element in root.QuerySelectorAll("a[target]").ToList())
                {
                    element.SetAttribute("target", "_blank");
                    element.SetAttribute("rel", "noopener noreferrer nofollow");
                }
            };

            return sanitizer;
        }

        private static readonly HashSet<string> BlockTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "li", "blockquote", "pre", "tr", "table", "ul", "ol", "hr", "br",
        };

        /// <summary>
        /// Плоский текст страницы: одна строка на абзац/пункт/строку таблицы. Используется для поиска,
        /// сниппетов и сравнения версий. Отмеченные пункты чек-листа помечаются «[x]», неотмеченные — «[ ]».
        /// </summary>
        public static string ToPlainText(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var parser = new HtmlParser();
            var document = parser.ParseDocument("<body>" + html + "</body>");
            var sb = new StringBuilder();
            Walk(document.Body!, sb);

            var lines = sb.ToString()
                .Split('\n')
                .Select(l => Regex.Replace(l, @"[ \t ]+", " ").Trim())
                .Where(l => l.Length > 0)
                .ToList();

            // Отметка пункта чек-листа и его текст (внутри li есть блоки) — в одну строку
            for (var i = lines.Count - 2; i >= 0; i--)
            {
                if (lines[i] is "[x]" or "[ ]")
                {
                    lines[i] = lines[i] + " " + lines[i + 1];
                    lines.RemoveAt(i + 1);
                }
            }
            return string.Join("\n", lines);
        }

        private static void Walk(INode node, StringBuilder sb)
        {
            foreach (var child in node.ChildNodes)
            {
                if (child is IText text)
                {
                    sb.Append(text.Data.Replace('\n', ' '));
                    continue;
                }

                if (child is not IElement element)
                    continue;

                var name = element.LocalName;
                if (name == "iframe")
                {
                    sb.Append("\n[Видео]\n");
                    continue;
                }
                if (name == "img")
                {
                    var alt = element.GetAttribute("alt");
                    sb.Append(string.IsNullOrWhiteSpace(alt) ? " [Изображение] " : $" [Изображение: {alt}] ");
                    continue;
                }
                if (name == "input")
                    continue;

                if (name == "tr")
                {
                    // Строка таблицы — одной строкой текста: «ячейка | ячейка | ячейка»
                    var cells = element.Children
                        .Where(c => c.LocalName is "td" or "th")
                        .Select(c => Regex.Replace(c.TextContent, @"\s+", " ").Trim());
                    sb.Append('\n').Append(string.Join(" | ", cells)).Append('\n');
                    continue;
                }

                var isBlock = BlockTags.Contains(name);
                if (isBlock)
                    sb.Append('\n');

                if (name == "li" && element.GetAttribute("data-type") == "taskItem")
                    sb.Append(element.GetAttribute("data-checked") == "true" ? "[x] " : "[ ] ");
                if (name == "hr")
                    sb.Append("———");

                Walk(element, sb);

                if (isBlock)
                    sb.Append('\n');
            }
        }

        /// <summary>Пункты чек-листов страницы (для переноса в описание задачи).</summary>
        public static List<(string Text, bool Checked)> ExtractChecklist(string? html)
        {
            var result = new List<(string, bool)>();
            if (string.IsNullOrWhiteSpace(html))
                return result;

            var document = new HtmlParser().ParseDocument("<body>" + html + "</body>");
            foreach (var li in document.QuerySelectorAll("li[data-type=taskItem]"))
            {
                // Текст только самого пункта, без вложенных подпунктов
                var clone = (IElement)li.Clone();
                foreach (var nested in clone.QuerySelectorAll("ul, ol").ToList())
                    nested.Remove();
                var text = Regex.Replace(clone.TextContent, @"\s+", " ").Trim();
                if (text.Length > 0)
                    result.Add((text, li.GetAttribute("data-checked") == "true"));
            }
            return result;
        }
    }
}
