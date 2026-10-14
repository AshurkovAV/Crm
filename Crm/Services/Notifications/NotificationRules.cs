using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;

namespace Crm.Services.Notifications
{
    /// <summary>Участник компании для расчёта получателей.</summary>
    public sealed record CompanyMember(int UserId, bool IsManager);

    /// <summary>
    /// Кому и куда слать уведомления (чистые функции — покрыты тестами).
    /// </summary>
    public static class NotificationRules
    {
        /// <summary>
        /// Получатель уведомления о задаче: исполнитель, если он есть, это не тот, кто сам себе назначил/изменил
        /// задачу, и он состоит в текущей компании того, кто действует.
        /// </summary>
        public static int? TaskRecipient(int? assigneeUserId, int actorUserId, ISet<int> companyMemberIds)
        {
            if (assigneeUserId is not int assignee || assignee <= 0)
                return null;
            if (assignee == actorUserId)
                return null;
            return companyMemberIds.Contains(assignee) ? assignee : null;
        }

        /// <summary>
        /// Кто может читать раздел — те же правила, что в WikiAccess.CanRead: руководители всегда;
        /// при доступе «Все сотрудники» — все участники; при «Только выбранные» — из списка доступа и редакторы раздела.
        /// Автор (excludeUserId) исключается.
        /// </summary>
        public static IReadOnlyList<int> SectionReaders(
            int companyId,
            int sectionId,
            string? accessMode,
            IEnumerable<CompanyMember> members,
            ISet<int> grantedUserIds,
            ISet<int> sectionEditorIds,
            int? excludeUserId)
        {
            var result = new List<int>();
            foreach (var m in members.GroupBy(m => m.UserId).Select(g => new CompanyMember(g.Key, g.Any(x => x.IsManager))))
            {
                if (m.UserId == excludeUserId)
                    continue;
                var ctx = new WikiUserContext
                {
                    UserId = m.UserId,
                    CompanyId = companyId,
                    IsManager = m.IsManager,
                    GrantedSectionIds = grantedUserIds.Contains(m.UserId) ? new HashSet<int> { sectionId } : new HashSet<int>(),
                    EditorSectionIds = sectionEditorIds.Contains(m.UserId) ? new HashSet<int> { sectionId } : new HashSet<int>(),
                };
                if (WikiAccess.CanRead(ctx, companyId, sectionId, accessMode))
                    result.Add(m.UserId);
            }
            return result;
        }

        /// <summary>Каналы по настройкам пользователя (нет настроек — всё включено) и по наличию адреса / привязки MAX.</summary>
        public static (bool Email, bool Max) Channels(NotificationPreference? prefs, string kind, bool hasEmail, bool maxLinked)
        {
            var isWiki = kind == NotificationTemplates.KindWiki;
            var wantEmail = prefs == null || (isWiki ? prefs.WikiEmail : prefs.TaskEmail);
            var wantMax = prefs == null || (isWiki ? prefs.WikiMax : prefs.TaskMax);
            return (wantEmail && hasEmail, wantMax && maxLinked);
        }
    }
}
