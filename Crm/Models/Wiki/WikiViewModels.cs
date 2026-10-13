using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Services.Wiki;

namespace Crm.Models.Wiki
{
    /// <summary>Общие данные шапки/левой панели базы знаний.</summary>
    public abstract class WikiViewModelBase
    {
        public WikiUserContext Ctx { get; set; } = null!;
        public List<WikiSectionInfo> Sections { get; set; } = new();
        public string? Query { get; set; }
        public int StaleCount { get; set; }
        public bool CanManage => Ctx.IsManager;
    }

    public class WikiHomeViewModel : WikiViewModelBase
    {
        public bool NoCompany { get; set; }
        public List<WikiPageListItem> Pinned { get; set; } = new();
        public List<WikiPageListItem> Favorites { get; set; } = new();
        public List<WikiPageListItem> Recent { get; set; } = new();
        public IReadOnlyList<WikiTemplate> Templates => WikiTemplates.All;
    }

    public class WikiTreeViewModel
    {
        public WikiSectionInfo Section { get; set; } = null!;
        public List<WikiTreeItem> Items { get; set; } = new();
        public int? CurrentPageId { get; set; }
        public bool CanEdit { get; set; }

        public IEnumerable<WikiTreeItem> Children(int? parentId)
            => Items.Where(i => i.ParentId == parentId).OrderBy(i => i.SortOrder).ThenBy(i => i.Id);

        /// <summary>Предки текущей страницы (чтобы раскрыть их в дереве).</summary>
        public HashSet<int> OpenIds()
        {
            var open = new HashSet<int>();
            var byId = Items.ToDictionary(i => i.Id);
            var cursor = CurrentPageId;
            var guard = 0;
            while (cursor.HasValue && byId.TryGetValue(cursor.Value, out var item) && guard++ < 10)
            {
                open.Add(item.Id);
                cursor = item.ParentId;
            }
            return open;
        }

        public int Depth(int id)
        {
            var byId = Items.ToDictionary(i => i.Id);
            var depth = 0;
            int? cursor = id;
            while (cursor.HasValue && byId.TryGetValue(cursor.Value, out var item) && depth < 10)
            {
                depth++;
                cursor = item.ParentId;
            }
            return depth;
        }
    }

    public class WikiSectionViewModel : WikiViewModelBase
    {
        public WikiSectionInfo Section { get; set; } = null!;
        public WikiTreeViewModel Tree { get; set; } = null!;
        public List<WikiPageListItem> Pinned { get; set; } = new();
        public IReadOnlyList<WikiTemplate> Templates => WikiTemplates.All;
    }

    public record WikiCrumb(string Title, string? Url);

    public class WikiPageViewModel : WikiViewModelBase
    {
        public WikiPage Page { get; set; } = null!;
        public WikiSectionInfo Section { get; set; } = null!;
        public WikiTreeViewModel Tree { get; set; } = null!;
        public List<WikiCrumb> Breadcrumbs { get; set; } = new();
        public bool CanEdit { get; set; }
        public bool IsFavorite { get; set; }
        public string? ModifiedByName { get; set; }
        public string? OwnerName { get; set; }
        public bool IsStale { get; set; }
        public bool CanConfirmReview { get; set; }
        public int Depth { get; set; }
        public List<WikiTreeItem> Children { get; set; } = new();
        public IReadOnlyList<WikiTemplate> Templates => WikiTemplates.All;
    }

    public class WikiEditViewModel : WikiViewModelBase
    {
        public WikiPage Page { get; set; } = null!;
        public WikiSectionInfo Section { get; set; } = null!;
        public List<WikiCrumb> Breadcrumbs { get; set; } = new();
    }

    public class WikiHistoryViewModel : WikiViewModelBase
    {
        public WikiPage Page { get; set; } = null!;
        public WikiSectionInfo Section { get; set; } = null!;
        public List<WikiCrumb> Breadcrumbs { get; set; } = new();
        public List<WikiVersionInfo> Versions { get; set; } = new();
        public bool CanEdit { get; set; }

        /// <summary>Открытая версия (просмотр) — или null.</summary>
        public WikiPageVersion? Selected { get; set; }
        public string? SelectedAuthor { get; set; }

        /// <summary>Сравнение: версия Selected против CompareWith (предыдущей по умолчанию).</summary>
        public int? CompareWith { get; set; }
        public List<WikiDiffLine>? Diff { get; set; }
        public bool TitleChanged { get; set; }
        public string? CompareTitle { get; set; }
    }

    public class WikiSearchResultView
    {
        public WikiSearchHit Hit { get; set; } = null!;
        public string TitleHtml { get; set; } = "";
        public string ExcerptHtml { get; set; } = "";
        public List<string> FilesHtml { get; set; } = new();
    }

    public class WikiSearchViewModel : WikiViewModelBase
    {
        public List<WikiSearchResultView> Results { get; set; } = new();
    }

    public class WikiReviewViewModel : WikiViewModelBase
    {
        public List<WikiPageListItem> Pages { get; set; } = new();
    }

    public class WikiSettingsViewModel : WikiViewModelBase
    {
        public List<WikiUserInfo> Members { get; set; } = new();
        public HashSet<int> CompanyEditorIds { get; set; } = new();
    }

    public static class WikiFormat
    {
        public static string Date(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        public static string Day(DateTime date) => date.ToString("dd.MM.yyyy");

        public static string Size(long bytes)
            => bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.#} МБ"
               : bytes >= 1024 ? $"{bytes / 1024d:0} КБ"
               : $"{bytes} Б";

        public static string Pages(int n)
        {
            var mod100 = n % 100;
            var mod10 = n % 10;
            var word = mod100 is >= 11 and <= 14 ? "страниц"
                : mod10 == 1 ? "страница"
                : mod10 is >= 2 and <= 4 ? "страницы"
                : "страниц";
            return $"{n} {word}";
        }

        public static string SectionIcon(string? icon) => string.IsNullOrWhiteSpace(icon) ? "📘" : icon;
    }
}
