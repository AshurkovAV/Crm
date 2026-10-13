using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

/// <summary>
/// Кто есть текущий пользователь в базе знаний своей текущей компании.
/// Строится из БД на каждый запрос (WikiRepository.GetContextAsync) и передаётся
/// во все методы репозитория — все выборки ограничены CompanyId из контекста.
/// </summary>
public sealed class WikiUserContext
{
    public int UserId { get; init; }

    public int CompanyId { get; init; }

    /// <summary>Руководитель компании: владелец (Company.OwnerId) или активный участник с ролью Admin.</summary>
    public bool IsManager { get; init; }

    /// <summary>Назначен редактором всей базы знаний компании (WikiEditor.SectionId = NULL).</summary>
    public bool IsCompanyEditor { get; init; }

    /// <summary>Разделы, где пользователь назначен редактором раздела.</summary>
    public IReadOnlySet<int> EditorSectionIds { get; init; } = new HashSet<int>();

    /// <summary>Разделы с доступом «Только выбранные», куда пользователь добавлен.</summary>
    public IReadOnlySet<int> GrantedSectionIds { get; init; } = new HashSet<int>();

    /// <summary>Может ли создавать страницы хоть где-то (для кнопок на главной базы знаний).</summary>
    public bool CanWriteAnywhere => IsManager || IsCompanyEditor || EditorSectionIds.Count > 0;
}

/// <summary>
/// Правила доступа базы знаний (чистые функции — покрыты тестами):
/// <list type="bullet">
/// <item>Читать раздел: руководитель; любой сотрудник, если доступ «Все сотрудники компании»;
/// при доступе «Только выбранные» — те, кто в списке доступа, и редакторы этого раздела.</item>
/// <item>Редактировать страницы раздела: только тот, кто может читать раздел и при этом руководитель,
/// редактор всей базы знаний или редактор этого раздела.</item>
/// <item>Управлять разделами, доступом и редакторами: только руководители.</item>
/// </list>
/// </summary>
public static class WikiAccess
{
    public static bool CanRead(WikiUserContext ctx, WikiSection section)
        => CanRead(ctx, section.CompanyId, section.Id, section.AccessMode);

    public static bool CanRead(WikiUserContext ctx, int sectionCompanyId, int sectionId, string? accessMode)
    {
        if (sectionCompanyId != ctx.CompanyId)
            return false;
        if (ctx.IsManager)
            return true;
        if (!string.Equals(accessMode, WikiSection.AccessSelected, StringComparison.OrdinalIgnoreCase))
            return true;
        return ctx.GrantedSectionIds.Contains(sectionId) || ctx.EditorSectionIds.Contains(sectionId);
    }

    public static bool CanEdit(WikiUserContext ctx, WikiSection section)
        => CanEdit(ctx, section.CompanyId, section.Id, section.AccessMode);

    public static bool CanEdit(WikiUserContext ctx, int sectionCompanyId, int sectionId, string? accessMode)
    {
        if (!CanRead(ctx, sectionCompanyId, sectionId, accessMode))
            return false;
        return ctx.IsManager || ctx.IsCompanyEditor || ctx.EditorSectionIds.Contains(sectionId);
    }

    public static bool CanManage(WikiUserContext ctx) => ctx.IsManager;

    /// <summary>Роль участника компании, дающая права руководителя (как и для смены режима работы).</summary>
    public static bool IsManagerRole(string? role)
        => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

    /// <summary>Максимальная глубина дерева страниц внутри раздела.</summary>
    public const int MaxDepth = 3;
}
