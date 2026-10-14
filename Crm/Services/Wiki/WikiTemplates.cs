using System.Text;

namespace Crm.Services.Wiki
{
    public sealed record WikiTemplate(string Key, string Title, string Icon, string Description, string Html, string Color = "slate");

    /// <summary>
    /// Шаблоны новой страницы. HTML в том же формате, что сохраняет редактор (Tiptap),
    /// поэтому сразу открывается в редакторе и проходит санитайзер без потерь.
    /// </summary>
    public static class WikiTemplates
    {
        public static readonly IReadOnlyList<WikiTemplate> All = new List<WikiTemplate>
        {
            new("blank", "Пустая", "fa-regular fa-file", "Начните с чистого листа", "", "slate"),

            new("instruction", "Инструкция", "fa-solid fa-list-ol", "Пошаговое руководство: что нужно, шаги, результат",
                H2("Зачем это нужно") +
                P("Коротко: какую задачу решает инструкция и для кого она.") +
                Callout("info", "Перед началом убедитесь, что у вас есть нужные доступы.") +
                H2("Что понадобится") +
                Ul("Доступ к …", "Файл / шаблон …") +
                H2("Шаги") +
                Ol("Откройте …", "Нажмите …", "Проверьте, что …") +
                H2("Результат") +
                P("Как понять, что всё сделано правильно.") +
                Callout("tip", "Если что-то пошло не так — напишите ответственному за страницу."), "indigo"),

            new("regulation", "Регламент", "fa-solid fa-scale-balanced", "Цель, роли, порядок действий и сроки",
                H2("Цель") +
                P("Что регулирует документ и почему это важно.") +
                H2("Область применения") +
                P("Кого касается регламент, с какой даты действует.") +
                H2("Роли и ответственность") +
                Table(new[] { "Роль", "Отвечает за" }, new[] { new[] { "Руководитель", "…" }, new[] { "Сотрудник", "…" } }) +
                H2("Порядок действий") +
                Ol("…", "…", "…") +
                H2("Сроки") +
                P("…") +
                Callout("warning", "Нарушение регламента обсуждается с руководителем."), "violet"),

            new("checklist", "Чек-лист", "fa-solid fa-square-check", "Пункты с галочками — для проверок и приёмки",
                P("Отмечайте пункты по мере выполнения.") +
                Tasks("Первый пункт", "Второй пункт", "Третий пункт", "Финальная проверка"), "green"),

            new("onboarding", "Онбординг новичка", "fa-solid fa-user-plus", "План первого дня и недели, контакты, ссылки",
                Callout("info", "Добро пожаловать в команду! Здесь всё, что нужно в первые дни.") +
                H2("Первый день") +
                Tasks("Получить доступы к почте и CRM", "Познакомиться с командой", "Прочитать раздел «Как мы работаем»") +
                H2("Первая неделя") +
                Tasks("Изучить основные регламенты", "Выполнить первую задачу с наставником") +
                H2("Контакты") +
                Table(new[] { "Вопрос", "К кому обращаться" }, new[] { new[] { "Доступы и техника", "…" }, new[] { "Задачи и приоритеты", "…" } }) +
                H2("Полезные ссылки") +
                Ul("…", "…"), "sky"),

            new("client", "Работа с клиентом", "fa-solid fa-handshake", "Этапы, скрипт звонка, частые вопросы",
                H2("Этапы работы") +
                Ol("Первый контакт", "Выявление потребностей", "Предложение", "Согласование и оплата", "Выполнение и сдача") +
                H2("Скрипт первого звонка") +
                Quote("Здравствуйте! Меня зовут …, компания … Удобно говорить?") +
                H2("Ответы на частые вопросы") +
                Table(new[] { "Вопрос клиента", "Ответ" }, new[] { new[] { "Сколько стоит?", "…" }, new[] { "Какие сроки?", "…" } }) +
                Callout("warning", "Не обещайте сроки, не согласовав их с производством.") +
                H2("Чек-лист после встречи") +
                Tasks("Внести итоги в карточку сделки", "Отправить письмо с договорённостями", "Поставить задачу на следующий шаг"), "teal"),
        };

        public static WikiTemplate? Find(string? key) => All.FirstOrDefault(t => t.Key == key);

        private static string E(string text) => System.Net.WebUtility.HtmlEncode(text);

        private static string H2(string text) => $"<h2>{E(text)}</h2>";

        private static string P(string text) => $"<p>{E(text)}</p>";

        private static string Quote(string text) => $"<blockquote><p>{E(text)}</p></blockquote>";

        private static string Callout(string kind, string text) => $"<div data-callout=\"{kind}\"><p>{E(text)}</p></div>";

        private static string Ul(params string[] items) => "<ul>" + string.Concat(items.Select(i => $"<li><p>{E(i)}</p></li>")) + "</ul>";

        private static string Ol(params string[] items) => "<ol>" + string.Concat(items.Select(i => $"<li><p>{E(i)}</p></li>")) + "</ol>";

        private static string Tasks(params string[] items)
            => "<ul data-type=\"taskList\">"
               + string.Concat(items.Select(i =>
                   $"<li data-checked=\"false\" data-type=\"taskItem\"><label><input type=\"checkbox\"><span></span></label><div><p>{E(i)}</p></div></li>"))
               + "</ul>";

        private static string Table(string[] header, string[][] rows)
        {
            var sb = new StringBuilder("<table><tbody><tr>");
            foreach (var h in header)
                sb.Append($"<th colspan=\"1\" rowspan=\"1\"><p>{E(h)}</p></th>");
            sb.Append("</tr>");
            foreach (var row in rows)
            {
                sb.Append("<tr>");
                foreach (var cell in row)
                    sb.Append($"<td colspan=\"1\" rowspan=\"1\"><p>{E(cell)}</p></td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        }
    }
}
