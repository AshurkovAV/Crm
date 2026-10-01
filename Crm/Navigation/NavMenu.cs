namespace Crm.Navigation
{
    public record NavItem(
        string Key,
        string Title,
        string Icon,           // класс Font Awesome 6, например "fa-solid fa-list-check"
        string Url,
        string[] Controllers,  // контроллеры, при которых пункт подсвечивается как активный
        string[] Modes);       // в каких режимах пункт виден (WorkspaceMode.*)

    public record NavGroup(string Title, IReadOnlyList<NavItem> Items);

    /// <summary>
    /// Единое описание левого меню. Шапка/сайдбар рендерят его, фильтр режима
    /// (WorkspaceModeFilter) по нему же решает, какие разделы доступны в режиме «Задачи».
    /// </summary>
    public static class NavMenu
    {
        private static readonly string[] Both = { WorkspaceMode.Full, WorkspaceMode.Tasks };
        private static readonly string[] FullOnly = { WorkspaceMode.Full };

        public static readonly IReadOnlyList<NavGroup> Groups = new List<NavGroup>
        {
            new("Работа", new List<NavItem>
            {
                new("home",      "Главная",  "fa-solid fa-house",        "/",          new[] { "Home", "Dashboard" }, Both),
                new("tasks",     "Задачи",   "fa-solid fa-list-check",   "/Tasks",     new[] { "Tasks" },             Both),
                new("projects",  "Проекты",  "fa-solid fa-folder",       "/Project",   new[] { "Project", "Projects" }, Both),
                new("workflows", "Потоки",   "fa-solid fa-diagram-project", "/Workflows", new[] { "Workflows" },     Both),
            }),
            new("Продажи", new List<NavItem>
            {
                new("deals",      "Сделки",      "fa-solid fa-handshake",  "/Deals",      new[] { "Deals", "Crm" }, FullOnly),
                new("contacts",   "Контакты",    "fa-solid fa-address-book", "/Contacts", new[] { "Contacts" },     FullOnly),
                new("sales",      "Продажи",     "fa-solid fa-cart-shopping", "/Sales",   new[] { "Sales" },        FullOnly),
                new("calculator", "Калькулятор", "fa-solid fa-calculator", "/Calculator", new[] { "Calculator" },   FullOnly),
            }),
            new("Производство", new List<NavItem>
            {
                new("warehouse",   "Склад",      "fa-solid fa-boxes-stacked", "/Warehouse",   new[] { "Warehouse" },   FullOnly),
                new("contractors", "Подрядчики", "fa-solid fa-truck",         "/Contractors", new[] { "Contractors" }, FullOnly),
            }),
            new("Команда", new List<NavItem>
            {
                new("company", "Сотрудники", "fa-solid fa-users",   "/company", new[] { "Company" }, Both),
                new("vault",   "Пароли",     "fa-solid fa-key",     "/Vault",   new[] { "Vault" },   Both),
                new("chat",    "Чат",        "fa-regular fa-comment", "/Online", new[] { "Online" }, Both),
            }),
        };

        /// <summary>Группы и пункты, видимые в режиме (пустые группы отброшены).</summary>
        public static IReadOnlyList<NavGroup> For(string mode)
        {
            mode = WorkspaceMode.Normalize(mode);
            return Groups
                .Select(g => new NavGroup(g.Title, g.Items.Where(i => i.Modes.Contains(mode)).ToList()))
                .Where(g => g.Items.Count > 0)
                .ToList();
        }

        /// <summary>Контроллеры, недоступные в режиме (для фильтра-перенаправления).</summary>
        public static IReadOnlySet<string> HiddenControllers(string mode)
        {
            mode = WorkspaceMode.Normalize(mode);
            return Groups.SelectMany(g => g.Items)
                .Where(i => !i.Modes.Contains(mode))
                .SelectMany(i => i.Controllers)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
