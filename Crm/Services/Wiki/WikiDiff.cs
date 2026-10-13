namespace Crm.Services.Wiki
{
    public enum WikiDiffKind { Same, Added, Removed }

    public sealed record WikiDiffLine(WikiDiffKind Kind, string Text);

    /// <summary>
    /// Построчное сравнение двух версий (по плоскому тексту страницы, см. WikiHtml.ToPlainText).
    /// Классический LCS; для очень длинных страниц (больше MaxCells сравнений) — упрощённо:
    /// общие начало и конец + блок «удалено/добавлено» между ними.
    /// </summary>
    public static class WikiDiff
    {
        private const long MaxCells = 4_000_000;

        public static List<WikiDiffLine> Compare(string? oldText, string? newText)
        {
            var a = Split(oldText);
            var b = Split(newText);

            // Общие начало и конец сравнивать не нужно
            var prefix = 0;
            while (prefix < a.Length && prefix < b.Length && a[prefix] == b[prefix])
                prefix++;
            var suffix = 0;
            while (suffix < a.Length - prefix && suffix < b.Length - prefix && a[a.Length - 1 - suffix] == b[b.Length - 1 - suffix])
                suffix++;

            var result = new List<WikiDiffLine>();
            for (var i = 0; i < prefix; i++)
                result.Add(new WikiDiffLine(WikiDiffKind.Same, a[i]));

            var midA = a[prefix..(a.Length - suffix)];
            var midB = b[prefix..(b.Length - suffix)];
            if ((long)midA.Length * midB.Length > MaxCells)
            {
                result.AddRange(midA.Select(l => new WikiDiffLine(WikiDiffKind.Removed, l)));
                result.AddRange(midB.Select(l => new WikiDiffLine(WikiDiffKind.Added, l)));
            }
            else
            {
                result.AddRange(Lcs(midA, midB));
            }

            for (var i = a.Length - suffix; i < a.Length; i++)
                result.Add(new WikiDiffLine(WikiDiffKind.Same, a[i]));
            return result;
        }

        private static string[] Split(string? text)
            => string.IsNullOrEmpty(text) ? Array.Empty<string>() : text.Replace("\r", "").Split('\n');

        private static IEnumerable<WikiDiffLine> Lcs(string[] a, string[] b)
        {
            var n = a.Length;
            var m = b.Length;
            var table = new int[n + 1, m + 1];
            for (var i = n - 1; i >= 0; i--)
                for (var j = m - 1; j >= 0; j--)
                    table[i, j] = a[i] == b[j] ? table[i + 1, j + 1] + 1 : Math.Max(table[i + 1, j], table[i, j + 1]);

            var x = 0;
            var y = 0;
            var lines = new List<WikiDiffLine>();
            while (x < n && y < m)
            {
                if (a[x] == b[y])
                {
                    lines.Add(new WikiDiffLine(WikiDiffKind.Same, a[x]));
                    x++;
                    y++;
                }
                else if (table[x + 1, y] >= table[x, y + 1])
                {
                    lines.Add(new WikiDiffLine(WikiDiffKind.Removed, a[x++]));
                }
                else
                {
                    lines.Add(new WikiDiffLine(WikiDiffKind.Added, b[y++]));
                }
            }
            while (x < n)
                lines.Add(new WikiDiffLine(WikiDiffKind.Removed, a[x++]));
            while (y < m)
                lines.Add(new WikiDiffLine(WikiDiffKind.Added, b[y++]));
            return lines;
        }
    }
}
