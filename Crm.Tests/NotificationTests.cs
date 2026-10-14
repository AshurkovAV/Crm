using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Services.Notifications;
using Xunit;

namespace Crm.Tests;

public class NotificationRuleTests
{
    private static readonly HashSet<int> Members = new() { 1, 2, 3, 4 };

    [Fact]
    public void Task_goes_to_assignee_only()
        => Assert.Equal(3, NotificationRules.TaskRecipient(assigneeUserId: 3, actorUserId: 1, Members));

    [Fact]
    public void Self_assignment_is_not_notified()
        => Assert.Null(NotificationRules.TaskRecipient(1, 1, Members));

    [Fact]
    public void No_assignee_no_notification()
        => Assert.Null(NotificationRules.TaskRecipient(null, 1, Members));

    [Fact]
    public void Assignee_from_another_company_is_not_notified()
        => Assert.Null(NotificationRules.TaskRecipient(5, 1, Members));

    private static readonly List<CompanyMember> Company = new()
    {
        new(1, IsManager: true),
        new(2, false),
        new(3, false),
        new(4, false),
    };

    [Fact]
    public void Open_section_notifies_everyone_except_author()
    {
        var readers = NotificationRules.SectionReaders(1, 10, WikiSection.AccessAll, Company, new HashSet<int>(), new HashSet<int>(), excludeUserId: 2);
        Assert.Equal(new[] { 1, 3, 4 }, readers.OrderBy(x => x));
    }

    [Fact]
    public void Restricted_section_notifies_only_granted_editors_and_managers()
    {
        var readers = NotificationRules.SectionReaders(1, 10, WikiSection.AccessSelected, Company,
            grantedUserIds: new HashSet<int> { 3 }, sectionEditorIds: new HashSet<int> { 4 }, excludeUserId: null);
        Assert.Equal(new[] { 1, 3, 4 }, readers.OrderBy(x => x));
        Assert.DoesNotContain(2, readers);
    }

    [Fact]
    public void Author_is_excluded_even_when_manager()
    {
        var readers = NotificationRules.SectionReaders(1, 10, WikiSection.AccessSelected, Company, new HashSet<int>(), new HashSet<int>(), excludeUserId: 1);
        Assert.Empty(readers);
    }

    [Fact]
    public void No_preferences_means_all_available_channels()
    {
        Assert.Equal((true, true), NotificationRules.Channels(null, NotificationTemplates.KindTaskAssigned, hasEmail: true, maxLinked: true));
        Assert.Equal((true, false), NotificationRules.Channels(null, NotificationTemplates.KindTaskAssigned, hasEmail: true, maxLinked: false));
        Assert.Equal((false, true), NotificationRules.Channels(null, NotificationTemplates.KindWiki, hasEmail: false, maxLinked: true));
    }

    [Fact]
    public void Preferences_are_respected_per_event()
    {
        var prefs = new NotificationPreference { TaskEmail = false, TaskMax = true, WikiEmail = true, WikiMax = false };
        Assert.Equal((false, true), NotificationRules.Channels(prefs, NotificationTemplates.KindTaskAssigned, true, true));
        Assert.Equal((false, true), NotificationRules.Channels(prefs, NotificationTemplates.KindTaskUpdated, true, true));
        Assert.Equal((true, false), NotificationRules.Channels(prefs, NotificationTemplates.KindWiki, true, true));
    }
}

public class NotificationTemplateTests
{
    private static TaskNotificationContext Ctx() => new()
    {
        Kind = TaskNotificationKind.Created,
        TaskId = 42,
        TaskName = "Замер <кухни> *срочно*",
        Description = "<p>Взять рулетку &amp; образцы</p><script>alert(1)</script>",
        Deadline = new DateTime(2026, 10, 20, 18, 0, 0),
        Priority = "Высокий",
        ProjectName = "Кухни",
        AuthorName = "Анна Смирнова",
        AssigneeUserId = 3,
        ActorUserId = 1,
    };

    [Fact]
    public void Task_email_has_details_button_and_encoded_values()
    {
        var m = NotificationTemplates.Task(Ctx(), "https://crm.example.ru/");
        Assert.Equal("https://crm.example.ru/Tasks?openTask=42", m.LinkUrl);
        Assert.Contains("Новая задача", m.Subject);
        Assert.Contains("Замер &lt;кухни&gt;", m.Html);
        Assert.DoesNotContain("<кухни>", m.Html);
        Assert.DoesNotContain("<script", m.Html);
        Assert.Contains("Открыть задачу", m.Html);
        Assert.Contains("href=\"https://crm.example.ru/Tasks?openTask=42\"", m.Html);
        Assert.Contains("Кухни", m.Html);
        Assert.Contains("20 октября 2026, 18:00", m.Html);
        Assert.Contains("Анна Смирнова", m.Html);
        Assert.Contains("Взять рулетку &amp; образцы", m.Html);
    }

    [Fact]
    public void Task_plain_text_and_max_versions()
    {
        var m = NotificationTemplates.Task(Ctx(), "https://crm.example.ru");
        Assert.Contains("Приоритет: Высокий", m.Text);
        Assert.Contains("Открыть задачу: https://crm.example.ru/Tasks?openTask=42", m.Text);
        Assert.DoesNotContain("<p>", m.Text);
        Assert.StartsWith("**Новая задача: Замер <кухни> срочно**", m.MaxText);
        Assert.Contains("Взять рулетку & образцы", m.MaxText);
    }

    [Fact]
    public void Updated_task_lists_changed_fields()
    {
        var c = Ctx();
        c.Kind = TaskNotificationKind.Updated;
        c.ChangedFields = new List<string> { "Срок", "Приоритет" };
        var m = NotificationTemplates.Task(c, "https://x");
        Assert.Equal(NotificationTemplates.KindTaskUpdated, m.Kind);
        Assert.Contains("Что изменилось: Срок, Приоритет", m.Text);
    }

    [Fact]
    public void Wiki_single_and_digest()
    {
        var one = NotificationTemplates.WikiDigest(new[] { new WikiDigestItem(7, "Регламент", "Производство", "Олег", false, "Текст") }, "Дуб", "https://x");
        Assert.Equal("https://x/Wiki/Page/7", one.LinkUrl);
        Assert.Contains("Новая статья в базе знаний: Регламент", one.Subject);

        var many = NotificationTemplates.WikiDigest(new[]
        {
            new WikiDigestItem(7, "Регламент", "Производство", "Олег", false, null),
            new WikiDigestItem(8, "Скрипт", "Продажи", "Иван", true, null),
        }, "Дуб", "https://x");
        Assert.Equal("https://x/Wiki", many.LinkUrl);
        Assert.Contains("2 статьи", many.Subject);
        Assert.Contains("https://x/Wiki/Page/8", many.Html);
        Assert.Contains("• Скрипт — Продажи (обновлено)", many.MaxText);
    }

    [Fact]
    public void Short_text_strips_html()
        => Assert.Equal("Строка 1\nСтрока 2", NotificationTemplates.ShortText("<p>Строка 1</p><p>Строка&nbsp;2</p>", 100));

    [Fact]
    public void Legacy_emoji_icons_are_resolved()
    {
        Assert.Equal("rocket", WikiIcons.Resolve("🚀").Key);
        Assert.Equal("industry", WikiIcons.Resolve("🏭").Key);
        Assert.Equal("book", WikiIcons.Resolve("что-то").Key);
        Assert.Equal("indigo", WikiIcons.NormalizeColor("<script>"));
    }
}
