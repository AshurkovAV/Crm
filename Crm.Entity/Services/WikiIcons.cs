namespace Crm.Entity.Services;

public sealed record WikiIcon(string Key, string Css, string Title);

public sealed record WikiColor(string Key, string Title, string Fg, string Bg);

/// <summary>
/// Значки разделов базы знаний: линейные иконки Font Awesome 6 в скруглённой плитке с мягким фоном.
/// В БД хранится ключ значка (WikiSection.Icon) и ключ цвета (WikiSection.Color). Старые записи с эмодзи
/// (до перехода на иконки) распознаются через Resolve — обратная совместимость без миграции данных.
/// </summary>
public static class WikiIcons
{
    public const string DefaultIcon = "book";
    public const string DefaultColor = "indigo";

    public static readonly IReadOnlyList<WikiIcon> Icons = new List<WikiIcon>
    {
        new("book", "fa-solid fa-book", "Книга"),
        new("book-open", "fa-solid fa-book-open", "Справочник"),
        new("rocket", "fa-solid fa-rocket", "Старт"),
        new("graduation", "fa-solid fa-graduation-cap", "Обучение"),
        new("compass", "fa-regular fa-compass", "Навигация"),
        new("flag", "fa-regular fa-flag", "Цели"),
        new("lightbulb", "fa-regular fa-lightbulb", "Идеи"),
        new("star", "fa-regular fa-star", "Важное"),
        new("handshake", "fa-regular fa-handshake", "Клиенты"),
        new("phone", "fa-solid fa-phone", "Звонки"),
        new("comments", "fa-regular fa-comments", "Общение"),
        new("users", "fa-solid fa-users", "Команда"),
        new("briefcase", "fa-solid fa-briefcase", "Работа"),
        new("chart", "fa-solid fa-chart-line", "Продажи"),
        new("coins", "fa-solid fa-coins", "Финансы"),
        new("receipt", "fa-solid fa-receipt", "Документы"),
        new("scale", "fa-solid fa-scale-balanced", "Регламенты"),
        new("list-check", "fa-solid fa-list-check", "Чек-листы"),
        new("industry", "fa-solid fa-industry", "Производство"),
        new("wrench", "fa-solid fa-screwdriver-wrench", "Инструменты"),
        new("boxes", "fa-solid fa-boxes-stacked", "Склад"),
        new("truck", "fa-solid fa-truck", "Доставка"),
        new("ruler", "fa-solid fa-ruler-combined", "Замеры"),
        new("palette", "fa-solid fa-palette", "Дизайн"),
        new("laptop", "fa-solid fa-laptop", "IT"),
        new("shield", "fa-solid fa-shield-halved", "Безопасность"),
        new("lock", "fa-solid fa-lock", "Закрытое"),
        new("heart", "fa-regular fa-heart", "Культура"),
        new("calendar", "fa-regular fa-calendar", "Планы"),
        new("question", "fa-regular fa-circle-question", "Вопросы"),
    };

    public static readonly IReadOnlyList<WikiColor> Colors = new List<WikiColor>
    {
        new("indigo", "Индиго", "#4f46e5", "#eef0ff"),
        new("blue", "Синий", "#2563eb", "#eaf2ff"),
        new("sky", "Голубой", "#0284c7", "#e6f6fd"),
        new("teal", "Бирюзовый", "#0f766e", "#e3f6f3"),
        new("green", "Зелёный", "#15803d", "#e8f7ed"),
        new("amber", "Янтарный", "#b45309", "#fdf4e3"),
        new("orange", "Оранжевый", "#c2410c", "#fdeee6"),
        new("rose", "Розовый", "#be123c", "#fdebef"),
        new("violet", "Фиолетовый", "#7c3aed", "#f2ecfe"),
        new("slate", "Графит", "#334155", "#eef1f5"),
    };

    private static readonly Dictionary<string, string> LegacyEmoji = new()
    {
        ["🚀"] = "rocket", ["🤝"] = "handshake", ["🏭"] = "industry", ["🔒"] = "lock",
        ["📘"] = "book", ["📗"] = "book", ["📕"] = "book", ["📙"] = "book", ["📒"] = "book",
        ["🧭"] = "compass", ["🛠️"] = "wrench", ["🛠"] = "wrench", ["💼"] = "briefcase", ["📞"] = "phone",
        ["🧾"] = "receipt", ["🚚"] = "truck", ["🎓"] = "graduation", ["💡"] = "lightbulb", ["⭐"] = "star",
    };

    /// <summary>Значок по ключу или старому эмодзи; неизвестное — книга.</summary>
    public static WikiIcon Resolve(string? icon)
    {
        var key = (icon ?? "").Trim();
        if (LegacyEmoji.TryGetValue(key, out var mapped))
            key = mapped;
        return Icons.FirstOrDefault(i => i.Key == key) ?? Icons[0];
    }

    public static WikiColor ResolveColor(string? color)
        => Colors.FirstOrDefault(c => c.Key == color) ?? Colors[0];

    /// <summary>Нормализует ввод пользователя: только известные ключи.</summary>
    public static string NormalizeIcon(string? icon) => Resolve(icon).Key;

    public static string NormalizeColor(string? color) => ResolveColor(color).Key;
}
