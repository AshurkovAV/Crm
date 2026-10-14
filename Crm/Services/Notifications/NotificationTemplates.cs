using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Crm.Services.Notifications
{
    /// <summary>
    /// Готовое уведомление для всех каналов: письмо (HTML + текст) и сообщение MAX (markdown).
    /// </summary>
    public sealed record NotificationMessage(
        string Kind,
        string Subject,
        string Html,
        string Text,
        string MaxText,
        string LinkUrl,
        string LinkText);

    /// <summary>Страница базы знаний в уведомлении о новых материалах.</summary>
    public sealed record WikiDigestItem(int PageId, string Title, string SectionName, string AuthorName, bool IsUpdate, string? Excerpt);

    /// <summary>
    /// Тексты уведомлений (русский). Все пользовательские значения в HTML кодируются, в MAX — очищаются
    /// от символов разметки. Письма — табличная вёрстка с инлайн-стилями (её понимают почтовые клиенты),
    /// в стиле нового дизайна CRM: светлый фон, белая карточка, акцент #4f46e5.
    /// </summary>
    public static class NotificationTemplates
    {
        public const string KindTaskAssigned = "TaskAssigned";
        public const string KindTaskUpdated = "TaskUpdated";
        public const string KindWiki = "WikiPages";

        public const string LinkedReply = "Готово, уведомления CRM подключены ✅\n\nСюда будут приходить новые задачи и новые статьи базы знаний. Настроить — в CRM → «Мой профиль» → «Уведомления».";
        public const string ExpiredReply = "Ссылка для подключения устарела или уже использована. Откройте CRM → «Мой профиль» → «Уведомления» → «Подключить MAX» ещё раз.";
        public const string HelloReply = "Это бот уведомлений CRM. Чтобы подключить уведомления, откройте CRM → «Мой профиль» → «Уведомления» → «Подключить MAX».";

        private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

        // =================================================================== задачи

        public static NotificationMessage Task(TaskNotificationContext c, string baseUrl)
        {
            var isNew = c.Kind == TaskNotificationKind.Created;
            var url = $"{baseUrl.TrimEnd('/')}/Tasks?openTask={c.TaskId}";
            var title = string.IsNullOrWhiteSpace(c.TaskName) ? $"Задача #{c.TaskId}" : c.TaskName.Trim();
            var heading = isNew ? "Вам назначена задача" : "Задача изменена";
            var intro = isNew
                ? $"{c.AuthorName ?? "Коллега"} назначил(а) вам задачу."
                : $"{c.AuthorName ?? "Коллега"} изменил(а) задачу, которую вы выполняете.";
            var description = ShortText(c.Description, 400);

            var facts = new List<(string Label, string Value, string? Color)>();
            if (!string.IsNullOrWhiteSpace(c.ProjectName)) facts.Add(("Проект", c.ProjectName.Trim(), null));
            if (c.Deadline.HasValue) facts.Add(("Срок", c.Deadline.Value.ToString("d MMMM yyyy, HH:mm", Ru), null));
            if (!string.IsNullOrWhiteSpace(c.Priority)) facts.Add(("Приоритет", c.Priority.Trim(), PriorityColor(c.Priority)));
            if (!string.IsNullOrWhiteSpace(c.AuthorName)) facts.Add(("Постановщик", c.AuthorName.Trim(), null));
            if (!isNew && c.ChangedFields.Count > 0) facts.Add(("Что изменилось", string.Join(", ", c.ChangedFields), null));

            // ----- HTML
            var body = new StringBuilder();
            body.Append(Paragraph(intro, "#5b6475"));
            body.Append($"<div style=\"margin:18px 0 6px;font-size:21px;line-height:1.3;font-weight:800;color:#0f172a;\">{E(title)}</div>");
            if (facts.Count > 0)
            {
                body.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:14px 0 4px;border-collapse:separate;border-spacing:0;background:#f8f9fc;border:1px solid #eef0f4;border-radius:14px;\">");
                for (var i = 0; i < facts.Count; i++)
                {
                    var (label, value, color) = facts[i];
                    var border = i < facts.Count - 1 ? "border-bottom:1px solid #eef0f4;" : "";
                    var valueHtml = color == null
                        ? E(value)
                        : $"<span style=\"display:inline-block;padding:2px 10px;border-radius:999px;background:{color}1a;color:{color};font-weight:700;\">{E(value)}</span>";
                    body.Append($"<tr><td style=\"padding:11px 16px;{border}font-size:13px;color:#5b6475;width:130px;vertical-align:top;\">{E(label)}</td>")
                        .Append($"<td style=\"padding:11px 16px;{border}font-size:14px;color:#0f172a;font-weight:600;\">{valueHtml}</td></tr>");
                }
                body.Append("</table>");
            }
            if (description.Length > 0)
            {
                body.Append("<div style=\"margin:16px 0 0;padding:14px 16px;border-left:3px solid #c7cbf5;background:#ffffff;color:#334155;font-size:14px;line-height:1.6;\">")
                    .Append(E(description).Replace("\n", "<br>"))
                    .Append("</div>");
            }

            var html = Layout(
                preheader: $"{heading}: {title}",
                badge: isNew ? "Новая задача" : "Изменение задачи",
                heading: heading,
                bodyHtml: body.ToString(),
                buttonText: "Открыть задачу",
                buttonUrl: url,
                footer: "Вы получили письмо, потому что вы исполнитель этой задачи. Отключить уведомления можно в CRM → «Мой профиль» → «Уведомления».");

            // ----- plain text
            var text = new StringBuilder();
            text.AppendLine(heading.ToUpper(Ru)).AppendLine().AppendLine(intro).AppendLine().AppendLine(title);
            foreach (var (label, value, _) in facts)
                text.AppendLine($"{label}: {value}");
            if (description.Length > 0)
                text.AppendLine().AppendLine(description);
            text.AppendLine().AppendLine($"Открыть задачу: {url}");

            // ----- MAX
            var max = new StringBuilder();
            max.Append("**").Append(isNew ? "Новая задача" : "Задача изменена").Append(": ").Append(Clean(title)).Append("**\n");
            foreach (var (label, value, _) in facts)
                max.Append(label).Append(": ").Append(Clean(value)).Append('\n');
            if (description.Length > 0)
                max.Append('\n').Append(Clean(ShortText(description, 300)));

            return new NotificationMessage(isNew ? KindTaskAssigned : KindTaskUpdated,
                $"{(isNew ? "Новая задача" : "Задача изменена")}: {title}", html, text.ToString().TrimEnd(), max.ToString().TrimEnd(), url, "Открыть задачу");
        }

        // =================================================================== база знаний

        public static NotificationMessage WikiDigest(IReadOnlyList<WikiDigestItem> items, string companyName, string baseUrl)
        {
            var root = baseUrl.TrimEnd('/');
            var single = items.Count == 1 ? items[0] : null;
            var url = single != null ? $"{root}/Wiki/Page/{single.PageId}" : $"{root}/Wiki";
            var subject = single != null
                ? (single.IsUpdate ? $"Обновлена статья: {single.Title}" : $"Новая статья в базе знаний: {single.Title}")
                : $"Новое в базе знаний: {items.Count} {Plural(items.Count, "статья", "статьи", "статей")}";
            var heading = single != null ? (single.IsUpdate ? "Статья обновлена" : "Новая статья") : "Новое в базе знаний";
            var intro = single != null
                ? $"{single.AuthorName} {(single.IsUpdate ? "обновил(а) статью" : "опубликовал(а) статью")} в разделе «{single.SectionName}»."
                : $"В базе знаний «{companyName}» появились материалы, которые вам доступны.";

            var body = new StringBuilder();
            body.Append(Paragraph(intro, "#5b6475"));
            body.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:16px 0 0;border-collapse:separate;border-spacing:0 8px;\">");
            foreach (var it in items)
            {
                var pageUrl = $"{root}/Wiki/Page/{it.PageId}";
                body.Append("<tr><td style=\"padding:14px 16px;background:#f8f9fc;border:1px solid #eef0f4;border-radius:14px;\">")
                    .Append($"<div style=\"font-size:12px;color:#8a93a6;margin-bottom:4px;\">{E(it.SectionName)} · {(it.IsUpdate ? "обновлено" : "новая статья")} · {E(it.AuthorName)}</div>")
                    .Append($"<a href=\"{E(pageUrl)}\" style=\"font-size:16px;font-weight:800;color:#0f172a;text-decoration:none;\">{E(it.Title)}</a>");
                if (!string.IsNullOrWhiteSpace(it.Excerpt))
                    body.Append($"<div style=\"margin-top:6px;font-size:13.5px;line-height:1.55;color:#334155;\">{E(ShortText(it.Excerpt, 220))}</div>");
                body.Append("</td></tr>");
            }
            body.Append("</table>");

            var html = Layout(
                preheader: subject,
                badge: "База знаний",
                heading: heading,
                bodyHtml: body.ToString(),
                buttonText: single != null ? "Открыть статью" : "Открыть базу знаний",
                buttonUrl: url,
                footer: "Вы получили письмо, потому что у вас есть доступ к этому разделу базы знаний. Отключить можно в CRM → «Мой профиль» → «Уведомления».");

            var text = new StringBuilder();
            text.AppendLine(heading.ToUpper(Ru)).AppendLine().AppendLine(intro).AppendLine();
            foreach (var it in items)
                text.AppendLine($"• {it.Title} ({it.SectionName}{(it.IsUpdate ? ", обновлено" : "")}) — {root}/Wiki/Page/{it.PageId}");
            text.AppendLine().AppendLine($"{(single != null ? "Открыть статью" : "Открыть базу знаний")}: {url}");

            var max = new StringBuilder();
            max.Append("**").Append(heading).Append("**\n");
            if (single != null)
            {
                max.Append(Clean(single.Title)).Append('\n')
                   .Append("Раздел: ").Append(Clean(single.SectionName)).Append(" · ").Append(Clean(single.AuthorName));
                if (!string.IsNullOrWhiteSpace(single.Excerpt))
                    max.Append("\n\n").Append(Clean(ShortText(single.Excerpt, 200)));
            }
            else
            {
                foreach (var it in items.Take(15))
                    max.Append("\n• ").Append(Clean(it.Title)).Append(" — ").Append(Clean(it.SectionName)).Append(it.IsUpdate ? " (обновлено)" : "");
                if (items.Count > 15)
                    max.Append("\n…и ещё ").Append(items.Count - 15);
            }

            return new NotificationMessage(KindWiki, subject, html, text.ToString().TrimEnd(), max.ToString().TrimEnd(), url,
                single != null ? "Открыть статью" : "Открыть базу знаний");
        }

        // =================================================================== общий макет письма

        public static string Layout(string preheader, string badge, string heading, string bodyHtml, string buttonText, string buttonUrl, string footer)
        {
            return $@"<!DOCTYPE html>
<html lang=""ru"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<meta name=""color-scheme"" content=""light"">
<title>{E(heading)}</title>
</head>
<body style=""margin:0;padding:0;background:#f4f5f8;"">
<div style=""display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;"">{E(preheader)}</div>
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f5f8;font-family:Manrope,-apple-system,'Segoe UI',Roboto,Arial,sans-serif;"">
<tr><td align=""center"" style=""padding:32px 16px;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;"">
    <tr><td style=""padding:0 4px 16px;"">
      <table role=""presentation"" cellpadding=""0"" cellspacing=""0""><tr>
        <td style=""width:28px;height:28px;background:#4f46e5;border-radius:8px;""></td>
        <td style=""padding-left:10px;font-size:17px;font-weight:800;color:#0f172a;letter-spacing:-.2px;"">CRM</td>
      </tr></table>
    </td></tr>
    <tr><td style=""background:#ffffff;border:1px solid #e4e7ee;border-radius:20px;padding:28px 28px 30px;"">
      <div style=""display:inline-block;padding:4px 12px;border-radius:999px;background:#eef0ff;color:#4f46e5;font-size:12px;font-weight:800;letter-spacing:.3px;"">{E(badge)}</div>
      <div style=""margin:14px 0 0;font-size:26px;line-height:1.2;font-weight:800;color:#0f172a;letter-spacing:-.4px;"">{E(heading)}</div>
      {bodyHtml}
      <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin:24px 0 0;""><tr>
        <td style=""border-radius:12px;background:#4f46e5;"">
          <a href=""{E(buttonUrl)}"" style=""display:inline-block;padding:13px 24px;font-size:15px;font-weight:800;color:#ffffff;text-decoration:none;border-radius:12px;"">{E(buttonText)} →</a>
        </td>
      </tr></table>
      <div style=""margin-top:14px;font-size:12px;color:#8a93a6;line-height:1.5;"">Кнопка не работает? Скопируйте ссылку: <a href=""{E(buttonUrl)}"" style=""color:#4f46e5;word-break:break-all;"">{E(buttonUrl)}</a></div>
    </td></tr>
    <tr><td style=""padding:16px 8px 0;font-size:12px;line-height:1.5;color:#8a93a6;text-align:center;"">{E(footer)}</td></tr>
  </table>
</td></tr>
</table>
</body>
</html>";
        }

        // =================================================================== помощники

        private static string Paragraph(string text, string color)
            => $"<div style=\"margin:10px 0 0;font-size:15px;line-height:1.55;color:{color};\">{E(text)}</div>";

        private static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        private static readonly Regex Markup = new(@"[*_~`\[\]]", RegexOptions.Compiled);

        /// <summary>Убирает символы разметки markdown MAX из пользовательских значений.</summary>
        public static string Clean(string? s) => Markup.Replace(s ?? "", "").Trim();

        /// <summary>Короткий текст из HTML описания: без тегов, не длиннее max символов.</summary>
        public static string ShortText(string? htmlOrText, int max)
        {
            if (string.IsNullOrWhiteSpace(htmlOrText))
                return string.Empty;
            var text = Regex.Replace(htmlOrText, @"<(br|/p|/li|/h\d|/div)[^>]*>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<[^>]+>", "");
            text = WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"[ \t ]+", " ");
            text = Regex.Replace(text, @"\s*\n\s*", "\n").Trim();
            return text.Length <= max ? text : text[..max].TrimEnd() + "…";
        }

        private static string PriorityColor(string? priority) => priority?.Trim() switch
        {
            "Высокий" => "#b91c1c",
            "Средний" => "#b45309",
            "Низкий" => "#15803d",
            _ => "#4f46e5",
        };

        private static string Plural(int n, string one, string few, string many)
        {
            var m100 = n % 100;
            var m10 = n % 10;
            if (m100 is >= 11 and <= 14) return many;
            return m10 == 1 ? one : m10 is >= 2 and <= 4 ? few : many;
        }
    }
}
