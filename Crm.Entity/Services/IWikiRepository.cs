using Crm.Entity.ModelsCrm;

namespace Crm.Entity.Services;

/// <summary>
/// База знаний компании. Каждый метод принимает WikiUserContext (кто и в какой компании) и сам
/// проверяет права (см. WikiAccess): чужая компания / недоступный раздел выглядят как «не найдено».
/// </summary>
public interface IWikiRepository
{
    Task<WikiUserContext?> GetContextAsync(int userId);

    // ----- участники и редакторы -----
    Task<List<WikiUserInfo>> GetMembersAsync(WikiUserContext ctx);
    Task<List<int>> GetCompanyEditorIdsAsync(WikiUserContext ctx);
    Task<bool> SetCompanyEditorsAsync(WikiUserContext ctx, IEnumerable<int> userIds);

    // ----- разделы -----
    Task<List<WikiSectionInfo>> GetSectionsAsync(WikiUserContext ctx);
    Task<WikiSection?> GetSectionAsync(WikiUserContext ctx, int sectionId);
    Task<WikiSection?> CreateSectionAsync(WikiUserContext ctx, string name, string? icon, string? description);
    Task<bool> UpdateSectionAsync(WikiUserContext ctx, int sectionId, string name, string? icon, string? description);
    Task<(bool Ok, string? Message, List<WikiFileInfo> FilesToDelete)> DeleteSectionAsync(WikiUserContext ctx, int sectionId);
    Task<bool> ShiftSectionAsync(WikiUserContext ctx, int sectionId, int delta);
    Task<(string AccessMode, List<int> AccessUserIds, List<int> EditorUserIds)?> GetSectionSettingsAsync(WikiUserContext ctx, int sectionId);
    Task<bool> SetSectionSettingsAsync(WikiUserContext ctx, int sectionId, string accessMode, IEnumerable<int> accessUserIds, IEnumerable<int> editorUserIds);

    // ----- страницы -----
    Task<List<WikiTreeItem>> GetTreeAsync(WikiUserContext ctx, int sectionId);
    Task<WikiPage?> GetPageAsync(WikiUserContext ctx, int pageId);
    Task<bool> CanEditPageAsync(WikiUserContext ctx, int pageId);
    Task<(WikiSaveStatus Status, int PageId, string? Message)> CreatePageAsync(WikiUserContext ctx, int sectionId, int? parentId, string title, string? html, string? text);
    Task<WikiSaveResult> SavePageAsync(WikiUserContext ctx, int pageId, int baseVersion, bool force, string title, string? json, string? html, string? text);
    Task<(bool Ok, string? Message)> MovePageAsync(WikiUserContext ctx, int pageId, int? newParentId, int index);
    Task<bool> ShiftPageAsync(WikiUserContext ctx, int pageId, int delta);
    Task<bool> DeletePageAsync(WikiUserContext ctx, int pageId);
    Task<bool> SetPinnedAsync(WikiUserContext ctx, int pageId, bool pinned);
    Task<List<WikiPageListItem>> GetPinnedAsync(WikiUserContext ctx, int? sectionId);
    Task<List<WikiPageListItem>> GetRecentAsync(WikiUserContext ctx, int take);
    Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds);

    // ----- избранное -----
    Task<bool?> ToggleFavoriteAsync(WikiUserContext ctx, int pageId);
    Task<bool> IsFavoriteAsync(WikiUserContext ctx, int pageId);
    Task<List<WikiPageListItem>> GetFavoritesAsync(WikiUserContext ctx);

    // ----- версии -----
    Task<List<WikiVersionInfo>> GetVersionsAsync(WikiUserContext ctx, int pageId);
    Task<WikiPageVersion?> GetVersionAsync(WikiUserContext ctx, int pageId, int versionNumber);
    Task<WikiSaveResult> RestoreVersionAsync(WikiUserContext ctx, int pageId, int versionNumber);

    // ----- актуальность -----
    Task<bool> SetReviewAsync(WikiUserContext ctx, int pageId, int? ownerUserId, DateTime? reviewDueDate);
    Task<bool> MarkReviewedAsync(WikiUserContext ctx, int pageId, DateTime? nextReviewDueDate);
    Task<List<WikiPageListItem>> GetStalePagesAsync(WikiUserContext ctx, DateTime today);

    // ----- поиск -----
    Task<List<WikiSearchHit>> SearchAsync(WikiUserContext ctx, string query, int take);

    // ----- вложения -----
    Task<List<WikiAttachmentInfo>> GetAttachmentsAsync(WikiUserContext ctx, int pageId);
    Task<(int AttachmentId, int VersionId)?> AddAttachmentAsync(WikiUserContext ctx, int pageId, bool isInline, string fileName, string storedName, string contentType, long sizeBytes);
    Task<int?> AddAttachmentVersionAsync(WikiUserContext ctx, int attachmentId, string fileName, string storedName, string contentType, long sizeBytes);
    Task<int?> GetAttachmentPageIdAsync(WikiUserContext ctx, int attachmentId);
    Task<bool> DeleteAttachmentAsync(WikiUserContext ctx, int attachmentId);
    Task<WikiFileInfo?> GetFileAsync(WikiUserContext ctx, int versionId);

    // ----- связь с задачами -----
    Task<(List<WikiLinkedTask> Tasks, int HiddenCount)> GetLinkedTasksAsync(WikiUserContext ctx, int pageId);
    Task<List<WikiPageListItem>?> GetLinkedPagesForTaskAsync(WikiUserContext ctx, int taskId);
    Task<bool> LinkTaskAsync(WikiUserContext ctx, int pageId, int taskId);
    Task<bool> UnlinkTaskAsync(WikiUserContext ctx, int pageId, int taskId);
}
