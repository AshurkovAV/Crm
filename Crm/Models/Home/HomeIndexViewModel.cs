using Crm.Entity.Services;
using Crm.Navigation;
using System.Globalization;

namespace Crm.Models.Home
{
    /// <summary>Модель главной страницы (Home/Index).</summary>
    public class HomeIndexViewModel
    {
        public bool HasOwnCompany { get; set; }
        public bool HasAnyCompany { get; set; }
        public string Mode { get; set; } = WorkspaceMode.Full;
        public bool IsFull => Mode == WorkspaceMode.Full;

        public DateTime Now { get; set; } = DateTime.Now;
        public HomeDashboardData Data { get; set; } = new();
        public List<HomeTeamMemberView> Team { get; set; } = new();

        public string GreetingName =>
            !string.IsNullOrWhiteSpace(Data.FirstName) ? Data.FirstName!.Trim()
            : !string.IsNullOrWhiteSpace(Data.DisplayName) ? Data.DisplayName!.Trim()
            : "коллега";

        private static readonly string[] Days = { "Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };
        private static readonly string[] MonthsGen = { "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };
        private static readonly string[] MonthsShort = { "янв", "фев", "мар", "апр", "мая", "июн", "июл", "авг", "сен", "окт", "ноя", "дек" };

        /// <summary>«Среда, 1 октября» — без зависимости от ICU на сервере.</summary>
        public string TodayText => $"{Days[(int)Now.DayOfWeek]}, {Now.Day} {MonthsGen[Now.Month - 1]}";

        public static string ShortDate(DateTime d, DateTime now)
        {
            var s = $"{d:dd} {MonthsShort[d.Month - 1]}";
            return d.Year != now.Year ? $"{s} {d.Year}" : s;
        }

        public static string Plural(int n, string one, string few, string many)
        {
            var n10 = n % 10; var n100 = n % 100;
            if (n10 == 1 && n100 != 11) return one;
            if (n10 >= 2 && n10 <= 4 && (n100 < 12 || n100 > 14)) return few;
            return many;
        }

        private static readonly CultureInfo Money = CreateMoneyCulture();
        private static CultureInfo CreateMoneyCulture()
        {
            var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            c.NumberFormat.NumberGroupSeparator = " ";
            c.NumberFormat.NumberDecimalSeparator = ",";
            return c;
        }

        /// <summary>«9 140 000 ₽» (без копеек).</summary>
        public static string Rub(decimal amount) => Math.Round(amount, 0).ToString("#,0", Money) + " ₽";

        /// <summary>Компактно: «9,1 млн ₽», «840 тыс ₽».</summary>
        public static string RubShort(decimal amount)
        {
            if (Math.Abs(amount) >= 1_000_000m) return (amount / 1_000_000m).ToString("0.#", Money) + " млн ₽";
            if (Math.Abs(amount) >= 10_000m) return (amount / 1_000m).ToString("0", Money) + " тыс ₽";
            return Rub(amount);
        }

        /// <summary>Однострочное резюме под приветствием (только реальные числа).</summary>
        public string Summary
        {
            get
            {
                var parts = new List<string> { TodayText };
                var d = Data;
                if (d.MyOpenTasks > 0)
                    parts.Add($"{d.MyOpenTasks} {Plural(d.MyOpenTasks, "задача", "задачи", "задач")} в работе");
                else
                    parts.Add("открытых задач нет");
                if (d.MyOverdueTasks > 0)
                    parts.Add($"{d.MyOverdueTasks} {Plural(d.MyOverdueTasks, "просрочена", "просрочены", "просрочено")}");
                if (IsFull && d.OrdersInProductionOverdue > 0)
                    parts.Add($"{d.OrdersInProductionOverdue} {Plural(d.OrdersInProductionOverdue, "заказ", "заказа", "заказов")} с истёкшим сроком");
                return string.Join(" · ", parts);
            }
        }
    }

    public class HomeTeamMemberView
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Initials { get; set; } = "?";
        public string? Position { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsMe { get; set; }
        /// <summary>Индекс цвета фона для инициалов (0..4).</summary>
        public int Tone { get; set; }
    }
}
