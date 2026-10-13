using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;

namespace Crm.Entity.Services;

/// <summary>
/// База знаний. Все выборки ограничены компанией из WikiUserContext и доступными разделами
/// (WikiAccess); любой write-метод заново проверяет право редактирования. Объект недоступного
/// раздела или чужой компании для вызывающего неотличим от несуществующего (null/false).
/// </summary>
public class WikiRepository : IWikiRepository
{
    private const int MaxSectionNameLength = 200;
    private const int MaxTitleLength = 300;

    // =====================================================================
    // Контекст пользователя
    // =====================================================================

    public async Task<WikiUserContext?> GetContextAsync(int userId)
    {
        await using var db = new CrmContext();

        var companyId = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.CurrentCompanyId)
            .FirstOrDefaultAsync();
        if (companyId is not int cid)
            return null;

        var company = await db.Companies
            .Where(c => c.Id == cid)
            .Select(c => new { c.OwnerId, c.IsActive })
            .FirstOrDefaultAsync();
        if (company == null || company.IsActive == false)
            return null;

        var roles = await db.CompanyUsers
            .Where(cu => cu.CompanyId == cid && cu.UserId == userId && cu.IsActive)
            .Select(cu => cu.Role)
            .ToListAsync();

        var isOwner = company.OwnerId == userId;
        if (!isOwner && roles.Count == 0)
            return null; // не участник (или исключён) — базы знаний этой компании для него нет

        var editorRows = await db.WikiEditors
            .Where(e => e.CompanyId == cid && e.UserId == userId)
            .Select(e => e.SectionId)
            .ToListAsync();

        var granted = await db.WikiSectionAccesses
            .Where(a => a.UserId == userId && db.WikiSections.Any(s => s.Id == a.SectionId && s.CompanyId == cid))
            .Select(a => a.SectionId)
            .ToListAsync();

        return new WikiUserContext
        {
            UserId = userId,
            CompanyId = cid,
            IsManager = isOwner || roles.Any(WikiAccess.IsManagerRole),
            IsCompanyEditor = editorRows.Any(id => id == null),
            EditorSectionIds = editorRows.Where(id => id.HasValue).Select(id => id!.Value).ToHashSet(),
            GrantedSectionIds = granted.ToHashSet(),
        };
    }

    // =====================================================================
    // Участники и редакторы
    // =====================================================================

    public async Task<List<WikiUserInfo>> GetMembersAsync(WikiUserContext ctx)
    {
        await using var db = new CrmContext();
        var ownerId = await db.Companies.Where(c => c.Id == ctx.CompanyId).Select(c => c.OwnerId).FirstAsync();

        var members = await db.CompanyUsers
            .Where(cu => cu.CompanyId == ctx.CompanyId && cu.IsActive)
            .Select(cu => new { cu.UserId, cu.Role, cu.User.DisplayName, cu.User.FirstName, cu.User.LastName, cu.User.DefaultEmail })
            .ToListAsync();

        var result = members
            .GroupBy(m => m.UserId)
            .Select(g =>
            {
                var m = g.First();
                return new WikiUserInfo
                {
                    UserId = m.UserId,
                    Name = BuildName(m.DisplayName, m.FirstName, m.LastName, m.DefaultEmail, m.UserId),
                    Email = m.DefaultEmail,
                    IsManager = m.UserId == ownerId || g.Any(x => WikiAccess.IsManagerRole(x.Role)),
                };
            })
            .ToList();

        if (result.All(r => r.UserId != ownerId))
        {
            var owner = await db.Users.Where(u => u.Id == ownerId)
                .Select(u => new { u.Id, u.DisplayName, u.FirstName, u.LastName, u.DefaultEmail })
                .FirstOrDefaultAsync();
            if (owner != null)
            {
                result.Add(new WikiUserInfo
                {
                    UserId = owner.Id,
                    Name = BuildName(owner.DisplayName, owner.FirstName, owner.LastName, owner.DefaultEmail, owner.Id),
                    Email = owner.DefaultEmail,
                    IsManager = true,
                });
            }
        }

        return result.OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task<List<int>> GetCompanyEditorIdsAsync(WikiUserContext ctx)
    {
        await using var db = new CrmContext();
        return await db.WikiEditors
            .Where(e => e.CompanyId == ctx.CompanyId && e.SectionId == null)
            .Select(e => e.UserId)
            .ToListAsync();
    }

    public async Task<bool> SetCompanyEditorsAsync(WikiUserContext ctx, IEnumerable<int> userIds)
    {
        if (!WikiAccess.CanManage(ctx))
            return false;

        await using var db = new CrmContext();
        var memberIds = await GetMemberIdsAsync(db, ctx.CompanyId);
        var wanted = userIds.Distinct().Where(memberIds.Contains).ToHashSet();

        var existing = await db.WikiEditors
            .Where(e => e.CompanyId == ctx.CompanyId && e.SectionId == null)
            .ToListAsync();
        db.WikiEditors.RemoveRange(existing.Where(e => !wanted.Contains(e.UserId)));
        foreach (var id in wanted.Where(id => existing.All(e => e.UserId != id)))
        {
            db.WikiEditors.Add(new WikiEditor
            {
                CompanyId = ctx.CompanyId,
                SectionId = null,
                UserId = id,
                GrantedByUserId = ctx.UserId,
                GrantedDate = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
        return true;
    }

    // =====================================================================
    // Разделы
    // =====================================================================

    public async Task<List<WikiSectionInfo>> GetSectionsAsync(WikiUserContext ctx)
    {
        await using var db = new CrmContext();
        var sections = await ReadableSections(db, ctx)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                s.CompanyId,
                s.Name,
                s.Icon,
                s.Description,
                s.SortOrder,
                s.AccessMode,
                PageCount = s.Pages.Count(p => !p.IsDeleted),
                LastModified = s.Pages.Where(p => !p.IsDeleted).Max(p => (DateTime?)p.ModifiedDate),
            })
            .ToListAsync();

        return sections.Select(s => new WikiSectionInfo
        {
            Id = s.Id,
            Name = s.Name,
            Icon = s.Icon,
            Description = s.Description,
            SortOrder = s.SortOrder,
            AccessMode = s.AccessMode,
            PageCount = s.PageCount,
            LastModified = s.LastModified,
            CanEdit = WikiAccess.CanEdit(ctx, s.CompanyId, s.Id, s.AccessMode),
        }).ToList();
    }

    public async Task<WikiSection?> GetSectionAsync(WikiUserContext ctx, int sectionId)
    {
        await using var db = new CrmContext();
        return await ReadableSections(db, ctx).AsNoTracking().FirstOrDefaultAsync(s => s.Id == sectionId);
    }

    public async Task<WikiSection?> CreateSectionAsync(WikiUserContext ctx, string name, string? icon, string? description)
    {
        if (!WikiAccess.CanManage(ctx))
            return null;

        await using var db = new CrmContext();
        var maxOrder = await db.WikiSections
            .Where(s => s.CompanyId == ctx.CompanyId)
            .MaxAsync(s => (int?)s.SortOrder) ?? -1;

        var section = new WikiSection
        {
            CompanyId = ctx.CompanyId,
            Name = Clip(name, MaxSectionNameLength),
            Icon = Clip(icon, 16),
            Description = Clip(description, 1000),
            SortOrder = maxOrder + 1,
            AccessMode = WikiSection.AccessAll,
            CreatedByUserId = ctx.UserId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
        };
        db.WikiSections.Add(section);
        await db.SaveChangesAsync();
        return section;
    }

    public async Task<bool> UpdateSectionAsync(WikiUserContext ctx, int sectionId, string name, string? icon, string? description)
    {
        if (!WikiAccess.CanManage(ctx))
            return false;

        await using var db = new CrmContext();
        var section = await db.WikiSections.FirstOrDefaultAsync(s => s.Id == sectionId && s.CompanyId == ctx.CompanyId);
        if (section == null)
            return false;

        section.Name = Clip(name, MaxSectionNameLength);
        section.Icon = Clip(icon, 16);
        section.Description = Clip(description, 1000);
        section.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Ok, string? Message, List<WikiFileInfo> FilesToDelete)> DeleteSectionAsync(WikiUserContext ctx, int sectionId)
    {
        var none = new List<WikiFileInfo>();
        if (!WikiAccess.CanManage(ctx))
            return (false, null, none);

        await using var db = new CrmContext();
        var section = await db.WikiSections.FirstOrDefaultAsync(s => s.Id == sectionId && s.CompanyId == ctx.CompanyId);
        if (section == null)
            return (false, null, none);

        if (await db.WikiPages.AnyAsync(p => p.SectionId == sectionId && !p.IsDeleted))
            return (false, "В разделе есть страницы. Сначала удалите или перенесите их.", none);

        // В разделе остались только удалённые страницы — удаляем их окончательно вместе с файлами.
        await using var tx = await db.Database.BeginTransactionAsync();
        var files = await db.WikiAttachmentVersions
            .Where(v => db.WikiAttachments.Any(a => a.Id == v.AttachmentId && db.WikiPages.Any(p => p.Id == a.PageId && p.SectionId == sectionId)))
            .Select(v => new WikiFileInfo { VersionId = v.Id, CompanyId = ctx.CompanyId, StoredName = v.StoredName, FileName = v.FileName, ContentType = v.ContentType })
            .ToListAsync();

        var deletedPages = await db.WikiPages.Where(p => p.SectionId == sectionId).ToListAsync();
        foreach (var p in deletedPages)
            p.ParentId = null;
        await db.SaveChangesAsync();
        db.WikiPages.RemoveRange(deletedPages);
        db.WikiSections.Remove(section);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, null, files);
    }

    public async Task<bool> ShiftSectionAsync(WikiUserContext ctx, int sectionId, int delta)
    {
        if (!WikiAccess.CanManage(ctx))
            return false;

        await using var db = new CrmContext();
        var sections = await db.WikiSections
            .Where(s => s.CompanyId == ctx.CompanyId)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .ToListAsync();
        var index = sections.FindIndex(s => s.Id == sectionId);
        if (index < 0)
            return false;

        var target = Math.Clamp(index + delta, 0, sections.Count - 1);
        var item = sections[index];
        sections.RemoveAt(index);
        sections.Insert(target, item);
        for (var i = 0; i < sections.Count; i++)
            sections[i].SortOrder = i;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<(string AccessMode, List<int> AccessUserIds, List<int> EditorUserIds)?> GetSectionSettingsAsync(WikiUserContext ctx, int sectionId)
    {
        if (!WikiAccess.CanManage(ctx))
            return null;

        await using var db = new CrmContext();
        var section = await db.WikiSections.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sectionId && s.CompanyId == ctx.CompanyId);
        if (section == null)
            return null;

        var access = await db.WikiSectionAccesses.Where(a => a.SectionId == sectionId).Select(a => a.UserId).ToListAsync();
        var editors = await db.WikiEditors.Where(e => e.CompanyId == ctx.CompanyId && e.SectionId == sectionId).Select(e => e.UserId).ToListAsync();
        return (section.AccessMode, access, editors);
    }

    public async Task<bool> SetSectionSettingsAsync(WikiUserContext ctx, int sectionId, string accessMode, IEnumerable<int> accessUserIds, IEnumerable<int> editorUserIds)
    {
        if (!WikiAccess.CanManage(ctx))
            return false;

        await using var db = new CrmContext();
        var section = await db.WikiSections.FirstOrDefaultAsync(s => s.Id == sectionId && s.CompanyId == ctx.CompanyId);
        if (section == null)
            return false;

        var memberIds = await GetMemberIdsAsync(db, ctx.CompanyId);
        var accessSet = accessUserIds.Distinct().Where(memberIds.Contains).ToHashSet();
        var editorSet = editorUserIds.Distinct().Where(memberIds.Contains).ToHashSet();

        section.AccessMode = string.Equals(accessMode, WikiSection.AccessSelected, StringComparison.OrdinalIgnoreCase)
            ? WikiSection.AccessSelected
            : WikiSection.AccessAll;
        section.ModifiedDate = DateTime.UtcNow;

        var currentAccess = await db.WikiSectionAccesses.Where(a => a.SectionId == sectionId).ToListAsync();
        db.WikiSectionAccesses.RemoveRange(currentAccess.Where(a => !accessSet.Contains(a.UserId)));
        foreach (var id in accessSet.Where(id => currentAccess.All(a => a.UserId != id)))
            db.WikiSectionAccesses.Add(new WikiSectionAccess { SectionId = sectionId, UserId = id, GrantedDate = DateTime.UtcNow });

        var currentEditors = await db.WikiEditors.Where(e => e.CompanyId == ctx.CompanyId && e.SectionId == sectionId).ToListAsync();
        db.WikiEditors.RemoveRange(currentEditors.Where(e => !editorSet.Contains(e.UserId)));
        foreach (var id in editorSet.Where(id => currentEditors.All(e => e.UserId != id)))
        {
            db.WikiEditors.Add(new WikiEditor
            {
                CompanyId = ctx.CompanyId,
                SectionId = sectionId,
                UserId = id,
                GrantedByUserId = ctx.UserId,
                GrantedDate = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
        return true;
    }

    // =====================================================================
    // Страницы
    // =====================================================================

    public async Task<List<WikiTreeItem>> GetTreeAsync(WikiUserContext ctx, int sectionId)
    {
        await using var db = new CrmContext();
        if (!await ReadableSections(db, ctx).AnyAsync(s => s.Id == sectionId))
            return new List<WikiTreeItem>();

        var today = DateTime.UtcNow.Date;
        return await db.WikiPages
            .Where(p => p.SectionId == sectionId && p.CompanyId == ctx.CompanyId && !p.IsDeleted)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new WikiTreeItem
            {
                Id = p.Id,
                ParentId = p.ParentId,
                Title = p.Title,
                SortOrder = p.SortOrder,
                IsPinned = p.IsPinned,
                IsStale = p.ReviewDueDate != null && p.ReviewDueDate < today,
            })
            .ToListAsync();
    }

    public async Task<WikiPage?> GetPageAsync(WikiUserContext ctx, int pageId)
    {
        await using var db = new CrmContext();
        var page = await db.WikiPages.AsNoTracking()
            .Include(p => p.Section)
            .FirstOrDefaultAsync(p => p.Id == pageId && p.CompanyId == ctx.CompanyId && !p.IsDeleted);
        if (page == null || !WikiAccess.CanRead(ctx, page.Section))
            return null;
        return page;
    }

    public async Task<bool> CanEditPageAsync(WikiUserContext ctx, int pageId)
    {
        await using var db = new CrmContext();
        var page = await LoadEditablePageAsync(db, ctx, pageId);
        return page != null;
    }

    public async Task<(WikiSaveStatus Status, int PageId, string? Message)> CreatePageAsync(
        WikiUserContext ctx, int sectionId, int? parentId, string title, string? html, string? text)
    {
        await using var db = new CrmContext();
        var section = await db.WikiSections.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sectionId && s.CompanyId == ctx.CompanyId);
        if (section == null || !WikiAccess.CanRead(ctx, section))
            return (WikiSaveStatus.NotFound, 0, null);
        if (!WikiAccess.CanEdit(ctx, section))
            return (WikiSaveStatus.Forbidden, 0, null);

        var nodes = await db.WikiPages
            .Where(p => p.SectionId == sectionId && !p.IsDeleted)
            .Select(p => new { p.Id, p.ParentId })
            .ToListAsync();

        if (parentId.HasValue)
        {
            if (nodes.All(n => n.Id != parentId.Value))
                return (WikiSaveStatus.Invalid, 0, "Родительская страница не найдена.");
            var parents = nodes.ToDictionary(n => n.Id, n => n.ParentId);
            if (Depth(parentId.Value, parents) >= WikiAccess.MaxDepth)
                return (WikiSaveStatus.Invalid, 0, $"Вложенность страниц — не больше {WikiAccess.MaxDepth} уровней.");
        }

        var maxOrder = await db.WikiPages
            .Where(p => p.SectionId == sectionId && p.ParentId == parentId && !p.IsDeleted)
            .MaxAsync(p => (int?)p.SortOrder) ?? -1;

        var now = DateTime.UtcNow;
        var page = new WikiPage
        {
            CompanyId = ctx.CompanyId,
            SectionId = sectionId,
            ParentId = parentId,
            Title = Clip(title, MaxTitleLength),
            ContentHtml = html,
            ContentText = text,
            SortOrder = maxOrder + 1,
            Version = 1,
            OwnerUserId = ctx.UserId,
            CreatedByUserId = ctx.UserId,
            CreatedDate = now,
            ModifiedByUserId = ctx.UserId,
            ModifiedDate = now,
        };
        page.Versions.Add(new WikiPageVersion
        {
            VersionNumber = 1,
            Title = page.Title,
            ContentHtml = html,
            ContentText = text,
            Comment = "Страница создана",
            AuthorUserId = ctx.UserId,
            CreatedDate = now,
        });
        db.WikiPages.Add(page);
        await db.SaveChangesAsync();
        return (WikiSaveStatus.Ok, page.Id, null);
    }

    public async Task<WikiSaveResult> SavePageAsync(
        WikiUserContext ctx, int pageId, int baseVersion, bool force, string title, string? json, string? html, string? text)
    {
        await using var db = new CrmContext();
        var page = await db.WikiPages.Include(p => p.Section)
            .FirstOrDefaultAsync(p => p.Id == pageId && p.CompanyId == ctx.CompanyId && !p.IsDeleted);
        if (page == null || !WikiAccess.CanRead(ctx, page.Section))
            return WikiSaveResult.Of(WikiSaveStatus.NotFound);
        if (!WikiAccess.CanEdit(ctx, page.Section))
            return WikiSaveResult.Of(WikiSaveStatus.Forbidden);

        if (page.Version != baseVersion && !force)
            return await ConflictAsync(db, page);

        title = Clip(title, MaxTitleLength);
        if (page.Title == title && page.ContentHtml == html)
            return new WikiSaveResult { Status = WikiSaveStatus.Ok, Version = page.Version };

        return await AppendVersionAsync(db, ctx, page, title, json, html, text, null);
    }

    public async Task<(bool Ok, string? Message)> MovePageAsync(WikiUserContext ctx, int pageId, int? newParentId, int index)
    {
        await using var db = new CrmContext();
        var page = await LoadEditablePageAsync(db, ctx, pageId);
        if (page == null)
            return (false, null);

        var all = await db.WikiPages
            .Where(p => p.SectionId == page.SectionId && !p.IsDeleted)
            .ToListAsync();
        var parents = all.ToDictionary(p => p.Id, p => p.ParentId);
        var self = all.First(p => p.Id == pageId);

        if (newParentId.HasValue)
        {
            if (!parents.ContainsKey(newParentId.Value))
                return (false, "Страница назначения не найдена.");

            // Нельзя переносить страницу внутрь самой себя или своих потомков
            for (int? cursor = newParentId; cursor.HasValue; cursor = parents.GetValueOrDefault(cursor.Value))
            {
                if (cursor.Value == pageId)
                    return (false, "Нельзя перенести страницу внутрь её же подстраницы.");
            }

            var newDepth = Depth(newParentId.Value, parents) + 1;
            if (newDepth + SubtreeHeight(pageId, all.Select(p => (p.Id, p.ParentId)).ToList()) - 1 > WikiAccess.MaxDepth)
                return (false, $"Вложенность страниц — не больше {WikiAccess.MaxDepth} уровней.");
        }

        var oldParentId = self.ParentId;
        self.ParentId = newParentId;

        var siblings = all.Where(p => p.ParentId == newParentId && p.Id != pageId)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToList();
        siblings.Insert(Math.Clamp(index, 0, siblings.Count), self);
        for (var i = 0; i < siblings.Count; i++)
            siblings[i].SortOrder = i;

        if (oldParentId != newParentId)
        {
            var oldSiblings = all.Where(p => p.ParentId == oldParentId && p.Id != pageId)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToList();
            for (var i = 0; i < oldSiblings.Count; i++)
                oldSiblings[i].SortOrder = i;
        }

        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> ShiftPageAsync(WikiUserContext ctx, int pageId, int delta)
    {
        await using var db = new CrmContext();
        var page = await LoadEditablePageAsync(db, ctx, pageId);
        if (page == null)
            return false;

        var siblings = await db.WikiPages
            .Where(p => p.SectionId == page.SectionId && p.ParentId == page.ParentId && !p.IsDeleted)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .ToListAsync();
        var index = siblings.FindIndex(p => p.Id == pageId);
        var target = Math.Clamp(index + delta, 0, siblings.Count - 1);
        var item = siblings[index];
        siblings.RemoveAt(index);
        siblings.Insert(target, item);
        for (var i = 0; i < siblings.Count; i++)
            siblings[i].SortOrder = i;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePageAsync(WikiUserContext ctx, int pageId)
    {
        await using var db = new CrmContext();
        var page = await LoadEditablePageAsync(db, ctx, pageId);
        if (page == null)
            return false;

        var all = await db.WikiPages.Where(p => p.SectionId == page.SectionId && !p.IsDeleted).ToListAsync();
        var toDelete = new HashSet<int> { pageId };
        bool added;
        do
        {
            added = false;
            foreach (var p in all.Where(p => p.ParentId.HasValue && toDelete.Contains(p.ParentId.Value) && !toDelete.Contains(p.Id)))
            {
                toDelete.Add(p.Id);
                added = true;
            }
        } while (added);

        var now = DateTime.UtcNow;
        foreach (var p in all.Where(p => toDelete.Contains(p.Id)))
        {
            p.IsDeleted = true;
            p.DeletedDate = now;
            p.ModifiedByUserId = ctx.UserId;
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetPinnedAsync(WikiUserContext ctx, int pageId, bool pinned)
    {
        await using var db = new CrmContext();
        var page = await LoadEditablePageAsync(db, ctx, pageId);
        if (page == null)
            return false;
        page.IsPinned = pinned;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<WikiPageListItem>> GetPinnedAsync(WikiUserContext ctx, int? sectionId)
    {
        await using var db = new CrmContext();
        var sectionIds = await ReadableSections(db, ctx).Select(s => s.Id).ToListAsync();
        var query = db.WikiPages.Where(p => p.CompanyId == ctx.CompanyId && !p.IsDeleted && p.IsPinned && sectionIds.Contains(p.SectionId));
        if (sectionId.HasValue)
            query = query.Where(p => p.SectionId == sectionId.Value);
        return await ToListItems(db, query.OrderBy(p => p.Section.SortOrder).ThenBy(p => p.SortOrder).Take(30));
    }

    public async Task<List<WikiPageListItem>> GetRecentAsync(WikiUserContext ctx, int take)
    {
        await using var db = new CrmContext();
        var sectionIds = await ReadableSections(db, ctx).Select(s => s.Id).ToListAsync();
        var query = db.WikiPages
            .Where(p => p.CompanyId == ctx.CompanyId && !p.IsDeleted && sectionIds.Contains(p.SectionId))
            .OrderByDescending(p => p.ModifiedDate)
            .Take(take);
        return await ToListItems(db, query);
    }

    public async Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();
        await using var db = new CrmContext();
        var users = await db.Users.Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.FirstName, u.LastName, u.DefaultEmail })
            .ToListAsync();
        return users.ToDictionary(u => u.Id, u => BuildName(u.DisplayName, u.FirstName, u.LastName, u.DefaultEmail, u.Id));
    }

    // =====================================================================
    // Избранное
    // =====================================================================

    public async Task<bool?> ToggleFavoriteAsync(WikiUserContext ctx, int pageId)
    {
        if (await GetPageAsync(ctx, pageId) == null)
            return null;

        await using var db = new CrmContext();
        var existing = await db.WikiFavorites.FirstOrDefaultAsync(f => f.PageId == pageId && f.UserId == ctx.UserId);
        if (existing != null)
        {
            db.WikiFavorites.Remove(existing);
            await db.SaveChangesAsync();
            return false;
        }

        db.WikiFavorites.Add(new WikiFavorite { PageId = pageId, UserId = ctx.UserId, CreatedDate = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsFavoriteAsync(WikiUserContext ctx, int pageId)
    {
        await using var db = new CrmContext();
        return await db.WikiFavorites.AnyAsync(f => f.PageId == pageId && f.UserId == ctx.UserId);
    }

    public async Task<List<WikiPageListItem>> GetFavoritesAsync(WikiUserContext ctx)
    {
        await using var db = new CrmContext();
        var sectionIds = await ReadableSections(db, ctx).Select(s => s.Id).ToListAsync();
        var query = db.WikiPages
            .Where(p => p.CompanyId == ctx.CompanyId && !p.IsDeleted && sectionIds.Contains(p.SectionId)
                        && db.WikiFavorites.Any(f => f.PageId == p.Id && f.UserId == ctx.UserId))
            .OrderBy(p => p.Title)
            .Take(50);
        return await ToListItems(db, query);
    }

    // =====================================================================
    // Версии
    // =====================================================================

    public async Task<List<WikiVersionInfo>> GetVersionsAsync(WikiUserContext ctx, int pageId)
    {
        if (await GetPageAsync(ctx, pageId) == null)
            return new List<WikiVersionInfo>();

        await using var db = new CrmContext();
        var versions = await db.WikiPageVersions
            .Where(v => v.PageId == pageId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new { v.VersionNumber, v.Title, v.Comment, v.AuthorUserId, v.CreatedDate })
            .ToListAsync();
        var names = await GetUserNamesAsync(versions.Select(v => v.AuthorUserId));
        return versions.Select(v => new WikiVersionInfo
        {
            VersionNumber = v.VersionNumber,
            Title = v.Title,
            Comment = v.Comment,
            AuthorUserId = v.AuthorUserId,
            AuthorName = names.GetValueOrDefault(v.AuthorUserId),
            CreatedDate = v.CreatedDate,
        }).ToList();
    }

    public async Task<WikiPageVersion?> GetVersionAsync(WikiUserContext ctx, int pageId, int versionNumber)
    {
        if (await GetPageAsync(ctx, pageId) == null)
            return null;

        await using var db = new CrmContext();
        return await db.WikiPageVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.PageId == pageId && v.VersionNumber == versionNumber);
    }

    public async Task<WikiSaveResult> RestoreVersionAsync(WikiUserContext ctx, int pageId, int versionNumber)
    {
        await using var db = new CrmContext();
        var page = await LoadEditablePageAsync(db, ctx, pageId);
        if (page == null)
            return WikiSaveResult.Of(WikiSaveStatus.NotFound);

        var version = await db.WikiPageVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.PageId == pageId && v.VersionNumber == versionNumber);
        if (version == null)
            return WikiSaveResult.Of(WikiSaveStatus.NotFound);

        return await AppendVersionAsync(db, ctx, page, version.Title, version.ContentJson, version.ContentHtml, version.ContentText,
            $"Восстановлено из версии {versionNumber}");
    }

    // =====================================================================
    // Актуальность
    // =====================================================================

    public async Task<bool> SetReviewAsync(WikiUserContext ctx, int pageId, int? ownerUserId, DateTime? reviewDueDate)
    {
        await using var db = new CrmContext();
        var page = await LoadEditablePageAsync(db, ctx, pageId);
        if (page == null)
            return false;

        if (ownerUserId.HasValue && !(await GetMemberIdsAsync(db, ctx.CompanyId)).Contains(ownerUserId.Value))
            return false;

        page.OwnerUserId = ownerUserId;
        page.ReviewDueDate = reviewDueDate?.Date;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkReviewedAsync(WikiUserContext ctx, int pageId, DateTime? nextReviewDueDate)
    {
        await using var db = new CrmContext();
        var page = await db.WikiPages.Include(p => p.Section)
            .FirstOrDefaultAsync(p => p.Id == pageId && p.CompanyId == ctx.CompanyId && !p.IsDeleted);
        if (page == null || !WikiAccess.CanRead(ctx, page.Section))
            return false;

        // Подтвердить актуальность может редактор раздела или назначенный ответственный за страницу
        if (!WikiAccess.CanEdit(ctx, page.Section) && page.OwnerUserId != ctx.UserId)
            return false;

        var now = DateTime.UtcNow;
        page.LastReviewedDate = now;
        page.ReviewDueDate = (nextReviewDueDate ?? now.AddMonths(6)).Date;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<WikiPageListItem>> GetStalePagesAsync(WikiUserContext ctx, DateTime today)
    {
        await using var db = new CrmContext();
        var sections = await ReadableSections(db, ctx).Select(s => new { s.Id, s.CompanyId, s.AccessMode }).ToListAsync();
        var editable = sections.Where(s => WikiAccess.CanEdit(ctx, s.CompanyId, s.Id, s.AccessMode)).Select(s => s.Id).ToList();
        var readable = sections.Select(s => s.Id).ToList();
        var day = today.Date;

        var query = db.WikiPages
            .Where(p => p.CompanyId == ctx.CompanyId && !p.IsDeleted && p.ReviewDueDate != null && p.ReviewDueDate < day)
            .Where(p => editable.Contains(p.SectionId) || (p.OwnerUserId == ctx.UserId && readable.Contains(p.SectionId)))
            .OrderBy(p => p.ReviewDueDate)
            .Take(200);
        return await ToListItems(db, query);
    }

    // =====================================================================
    // Поиск
    // =====================================================================

    public async Task<List<WikiSearchHit>> SearchAsync(WikiUserContext ctx, string query, int take)
    {
        query = (query ?? string.Empty).Trim();
        if (query.Length < 2)
            return new List<WikiSearchHit>();
        if (query.Length > 100)
            query = query[..100];

        var pattern = "%" + EscapeLike(query) + "%";

        await using var db = new CrmContext();
        var sectionIds = await ReadableSections(db, ctx).Select(s => s.Id).ToListAsync();

        var pages = await db.WikiPages
            .Where(p => p.CompanyId == ctx.CompanyId && !p.IsDeleted && sectionIds.Contains(p.SectionId))
            .Where(p => EF.Functions.Like(p.Title, pattern, "\\")
                        || EF.Functions.Like(p.ContentText!, pattern, "\\")
                        || db.WikiAttachments.Any(a => a.PageId == p.Id && !a.IsDeleted && !a.IsInline
                            && a.Versions.Any(v => EF.Functions.Like(v.FileName, pattern, "\\"))))
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.SectionId,
                SectionName = p.Section.Name,
                SectionIcon = p.Section.Icon,
                p.ContentText,
                p.ModifiedDate,
                TitleMatch = EF.Functions.Like(p.Title, pattern, "\\"),
            })
            .OrderByDescending(p => p.TitleMatch)
            .ThenByDescending(p => p.ModifiedDate)
            .Take(take)
            .ToListAsync();

        var pageIds = pages.Select(p => p.Id).ToList();
        var files = await db.WikiAttachmentVersions
            .Where(v => EF.Functions.Like(v.FileName, pattern, "\\")
                        && db.WikiAttachments.Any(a => a.Id == v.AttachmentId && !a.IsDeleted && !a.IsInline && pageIds.Contains(a.PageId)))
            .Select(v => new { v.FileName, v.Attachment.PageId })
            .ToListAsync();

        return pages.Select(p => new WikiSearchHit
        {
            PageId = p.Id,
            Title = p.Title,
            SectionId = p.SectionId,
            SectionName = p.SectionName,
            SectionIcon = p.SectionIcon,
            ContentText = p.ContentText,
            ModifiedDate = p.ModifiedDate,
            TitleMatch = p.TitleMatch,
            MatchedFiles = files.Where(f => f.PageId == p.Id).Select(f => f.FileName).Distinct().Take(5).ToList(),
        }).ToList();
    }

    // =====================================================================
    // Вложения
    // =====================================================================

    public async Task<List<WikiAttachmentInfo>> GetAttachmentsAsync(WikiUserContext ctx, int pageId)
    {
        if (await GetPageAsync(ctx, pageId) == null)
            return new List<WikiAttachmentInfo>();

        await using var db = new CrmContext();
        var rows = await db.WikiAttachments
            .Where(a => a.PageId == pageId && a.CompanyId == ctx.CompanyId && !a.IsDeleted && !a.IsInline)
            .Select(a => new
            {
                a.Id,
                a.CreatedDate,
                Versions = a.Versions.OrderByDescending(v => v.VersionNumber)
                    .Select(v => new { v.Id, v.VersionNumber, v.FileName, v.ContentType, v.SizeBytes, v.UploadedDate, v.UploadedByUserId })
                    .ToList(),
            })
            .OrderBy(a => a.CreatedDate)
            .ToListAsync();

        var names = await GetUserNamesAsync(rows.SelectMany(r => r.Versions.Select(v => v.UploadedByUserId)));
        return rows.Where(r => r.Versions.Count > 0).Select(r =>
        {
            var current = r.Versions[0];
            return new WikiAttachmentInfo
            {
                Id = r.Id,
                CurrentVersionId = current.Id,
                VersionNumber = current.VersionNumber,
                FileName = current.FileName,
                ContentType = current.ContentType,
                SizeBytes = current.SizeBytes,
                UploadedDate = current.UploadedDate,
                UploadedByName = names.GetValueOrDefault(current.UploadedByUserId),
                History = r.Versions.Skip(1).Select(v => new WikiAttachmentVersionInfo
                {
                    Id = v.Id,
                    VersionNumber = v.VersionNumber,
                    FileName = v.FileName,
                    SizeBytes = v.SizeBytes,
                    UploadedDate = v.UploadedDate,
                    UploadedByName = names.GetValueOrDefault(v.UploadedByUserId),
                }).ToList(),
            };
        }).ToList();
    }

    public async Task<(int AttachmentId, int VersionId)?> AddAttachmentAsync(
        WikiUserContext ctx, int pageId, bool isInline, string fileName, string storedName, string contentType, long sizeBytes)
    {
        await using var db = new CrmContext();
        var page = await LoadEditablePageAsync(db, ctx, pageId);
        if (page == null)
            return null;

        var now = DateTime.UtcNow;
        var attachment = new WikiAttachment
        {
            CompanyId = ctx.CompanyId,
            PageId = pageId,
            IsInline = isInline,
            CreatedByUserId = ctx.UserId,
            CreatedDate = now,
        };
        var version = new WikiAttachmentVersion
        {
            VersionNumber = 1,
            FileName = Clip(fileName, 255),
            StoredName = storedName,
            ContentType = Clip(contentType, 150),
            SizeBytes = sizeBytes,
            UploadedByUserId = ctx.UserId,
            UploadedDate = now,
        };
        attachment.Versions.Add(version);
        db.WikiAttachments.Add(attachment);
        await db.SaveChangesAsync();
        return (attachment.Id, version.Id);
    }

    public async Task<int?> AddAttachmentVersionAsync(
        WikiUserContext ctx, int attachmentId, string fileName, string storedName, string contentType, long sizeBytes)
    {
        await using var db = new CrmContext();
        var attachment = await db.WikiAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.CompanyId == ctx.CompanyId && !a.IsDeleted && !a.IsInline);
        if (attachment == null || await LoadEditablePageAsync(db, ctx, attachment.PageId) == null)
            return null;

        var maxVersion = await db.WikiAttachmentVersions.Where(v => v.AttachmentId == attachmentId).MaxAsync(v => (int?)v.VersionNumber) ?? 0;
        var version = new WikiAttachmentVersion
        {
            AttachmentId = attachmentId,
            VersionNumber = maxVersion + 1,
            FileName = Clip(fileName, 255),
            StoredName = storedName,
            ContentType = Clip(contentType, 150),
            SizeBytes = sizeBytes,
            UploadedByUserId = ctx.UserId,
            UploadedDate = DateTime.UtcNow,
        };
        db.WikiAttachmentVersions.Add(version);
        await db.SaveChangesAsync();
        return version.Id;
    }

    public async Task<int?> GetAttachmentPageIdAsync(WikiUserContext ctx, int attachmentId)
    {
        await using var db = new CrmContext();
        return await db.WikiAttachments
            .Where(a => a.Id == attachmentId && a.CompanyId == ctx.CompanyId && !a.IsDeleted)
            .Select(a => (int?)a.PageId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteAttachmentAsync(WikiUserContext ctx, int attachmentId)
    {
        await using var db = new CrmContext();
        var attachment = await db.WikiAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.CompanyId == ctx.CompanyId && !a.IsDeleted);
        if (attachment == null || await LoadEditablePageAsync(db, ctx, attachment.PageId) == null)
            return false;

        attachment.IsDeleted = true;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<WikiFileInfo?> GetFileAsync(WikiUserContext ctx, int versionId)
    {
        await using var db = new CrmContext();
        var row = await db.WikiAttachmentVersions
            .Where(v => v.Id == versionId)
            .Select(v => new
            {
                v.Id,
                v.FileName,
                v.StoredName,
                v.ContentType,
                v.SizeBytes,
                v.Attachment.CompanyId,
                v.Attachment.PageId,
                v.Attachment.IsDeleted,
            })
            .FirstOrDefaultAsync();
        if (row == null || row.IsDeleted || row.CompanyId != ctx.CompanyId)
            return null;

        var page = await db.WikiPages.AsNoTracking().Include(p => p.Section)
            .FirstOrDefaultAsync(p => p.Id == row.PageId && p.CompanyId == ctx.CompanyId && !p.IsDeleted);
        if (page == null || !WikiAccess.CanRead(ctx, page.Section))
            return null;

        return new WikiFileInfo
        {
            VersionId = row.Id,
            CompanyId = row.CompanyId,
            FileName = row.FileName,
            StoredName = row.StoredName,
            ContentType = row.ContentType,
            SizeBytes = row.SizeBytes,
        };
    }

    // =====================================================================
    // Связь с задачами
    // =====================================================================

    public async Task<(List<WikiLinkedTask> Tasks, int HiddenCount)> GetLinkedTasksAsync(WikiUserContext ctx, int pageId)
    {
        if (await GetPageAsync(ctx, pageId) == null)
            return (new List<WikiLinkedTask>(), 0);

        await using var db = new CrmContext();
        var linkedIds = await db.WikiPageTasks.Where(l => l.PageId == pageId).Select(l => l.TaskId).ToListAsync();
        if (linkedIds.Count == 0)
            return (new List<WikiLinkedTask>(), 0);

        var tasks = await AccessibleTasks(db, ctx.UserId)
            .Where(t => linkedIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name, t.Status, t.Deadline, t.Assignee })
            .ToListAsync();
        var statusIds = tasks.Where(t => t.Status.HasValue).Select(t => t.Status!.Value).Distinct().ToList();
        var statuses = await db.RefStatuses.Where(s => statusIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name);
        var names = await GetUserNamesAsync(tasks.Where(t => t.Assignee.HasValue).Select(t => t.Assignee!.Value));

        var list = tasks.OrderByDescending(t => t.Id).Select(t => new WikiLinkedTask
        {
            TaskId = t.Id,
            Name = t.Name,
            Status = t.Status,
            StatusName = t.Status.HasValue ? statuses.GetValueOrDefault(t.Status.Value) : null,
            Deadline = t.Deadline,
            AssigneeName = t.Assignee.HasValue ? names.GetValueOrDefault(t.Assignee.Value) : null,
        }).ToList();
        return (list, linkedIds.Count - list.Count);
    }

    public async Task<List<WikiPageListItem>?> GetLinkedPagesForTaskAsync(WikiUserContext ctx, int taskId)
    {
        await using var db = new CrmContext();
        if (!await AccessibleTasks(db, ctx.UserId).AnyAsync(t => t.Id == taskId))
            return null;

        var sectionIds = await ReadableSections(db, ctx).Select(s => s.Id).ToListAsync();
        var query = db.WikiPages
            .Where(p => p.CompanyId == ctx.CompanyId && !p.IsDeleted && sectionIds.Contains(p.SectionId)
                        && db.WikiPageTasks.Any(l => l.PageId == p.Id && l.TaskId == taskId))
            .OrderBy(p => p.Title);
        return await ToListItems(db, query);
    }

    public async Task<bool> LinkTaskAsync(WikiUserContext ctx, int pageId, int taskId)
    {
        if (await GetPageAsync(ctx, pageId) == null)
            return false;

        await using var db = new CrmContext();
        if (!await AccessibleTasks(db, ctx.UserId).AnyAsync(t => t.Id == taskId))
            return false;

        if (!await db.WikiPageTasks.AnyAsync(l => l.PageId == pageId && l.TaskId == taskId))
        {
            db.WikiPageTasks.Add(new WikiPageTask { PageId = pageId, TaskId = taskId, CreatedByUserId = ctx.UserId, CreatedDate = DateTime.UtcNow });
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Параллельный запрос уже создал эту связь (уникальный индекс) — результат тот же.
            }
        }

        return true;
    }

    public async Task<bool> UnlinkTaskAsync(WikiUserContext ctx, int pageId, int taskId)
    {
        if (await GetPageAsync(ctx, pageId) == null)
            return false;

        await using var db = new CrmContext();
        if (!await AccessibleTasks(db, ctx.UserId).AnyAsync(t => t.Id == taskId))
            return false;

        var links = await db.WikiPageTasks.Where(l => l.PageId == pageId && l.TaskId == taskId).ToListAsync();
        db.WikiPageTasks.RemoveRange(links);
        await db.SaveChangesAsync();
        return true;
    }

    // =====================================================================
    // Вспомогательное
    // =====================================================================

    /// <summary>Разделы текущей компании, которые пользователь может читать (фильтр — в SQL).</summary>
    private static IQueryable<WikiSection> ReadableSections(CrmContext db, WikiUserContext ctx)
    {
        var query = db.WikiSections.Where(s => s.CompanyId == ctx.CompanyId);
        if (ctx.IsManager)
            return query;

        var allowed = ctx.GrantedSectionIds.Concat(ctx.EditorSectionIds).Distinct().ToList();
        return query.Where(s => s.AccessMode != WikiSection.AccessSelected || allowed.Contains(s.Id));
    }

    /// <summary>Задачи, видимые пользователю, — то же правило, что в списке задач (CrmRepository.GetTasksWithAccess).</summary>
    private static IQueryable<TaskCrm> AccessibleTasks(CrmContext db, int userId)
        => db.TaskCrms.Where(t =>
            db.ProjectUsers.Any(pu => pu.UserId == userId && pu.IsActive && pu.ProjectId == t.ProjectId)
            || t.Assignee == userId
            || t.Author == userId);

    private static async Task<WikiPage?> LoadEditablePageAsync(CrmContext db, WikiUserContext ctx, int pageId)
    {
        var page = await db.WikiPages.Include(p => p.Section)
            .FirstOrDefaultAsync(p => p.Id == pageId && p.CompanyId == ctx.CompanyId && !p.IsDeleted);
        return page != null && WikiAccess.CanEdit(ctx, page.Section) ? page : null;
    }

    private static async Task<HashSet<int>> GetMemberIdsAsync(CrmContext db, int companyId)
    {
        var ids = await db.CompanyUsers
            .Where(cu => cu.CompanyId == companyId && cu.IsActive)
            .Select(cu => cu.UserId)
            .ToListAsync();
        var ownerId = await db.Companies.Where(c => c.Id == companyId).Select(c => c.OwnerId).FirstAsync();
        ids.Add(ownerId);
        return ids.ToHashSet();
    }

    private async Task<WikiSaveResult> ConflictAsync(CrmContext db, WikiPage page)
    {
        var names = await GetUserNamesAsync(new[] { page.ModifiedByUserId });
        return new WikiSaveResult
        {
            Status = WikiSaveStatus.Conflict,
            Version = page.Version,
            ModifiedByName = names.GetValueOrDefault(page.ModifiedByUserId),
            ModifiedDate = page.ModifiedDate,
        };
    }

    private async Task<WikiSaveResult> AppendVersionAsync(
        CrmContext db, WikiUserContext ctx, WikiPage page, string title, string? json, string? html, string? text, string? comment)
    {
        var now = DateTime.UtcNow;
        page.Version += 1;
        page.Title = title;
        page.ContentJson = json;
        page.ContentHtml = html;
        page.ContentText = text;
        page.ModifiedByUserId = ctx.UserId;
        page.ModifiedDate = now;

        db.WikiPageVersions.Add(new WikiPageVersion
        {
            PageId = page.Id,
            VersionNumber = page.Version,
            Title = title,
            ContentJson = json,
            ContentHtml = html,
            ContentText = text,
            Comment = comment,
            AuthorUserId = ctx.UserId,
            CreatedDate = now,
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Кто-то сохранил ту же страницу одновременно (уникальный номер версии) — это конфликт.
            await using var fresh = new CrmContext();
            var current = await fresh.WikiPages.AsNoTracking().FirstAsync(p => p.Id == page.Id);
            return await ConflictAsync(fresh, current);
        }

        return new WikiSaveResult { Status = WikiSaveStatus.Ok, Version = page.Version };
    }

    private static async Task<List<WikiPageListItem>> ToListItems(CrmContext db, IQueryable<WikiPage> query)
    {
        var rows = await query
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.SectionId,
                SectionName = p.Section.Name,
                SectionIcon = p.Section.Icon,
                p.ModifiedDate,
                p.ModifiedByUserId,
                p.ReviewDueDate,
                p.OwnerUserId,
            })
            .ToListAsync();

        var userIds = rows.Select(r => r.ModifiedByUserId)
            .Concat(rows.Where(r => r.OwnerUserId.HasValue).Select(r => r.OwnerUserId!.Value))
            .Distinct().ToList();
        var users = await db.Users.Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.FirstName, u.LastName, u.DefaultEmail })
            .ToListAsync();
        var names = users.ToDictionary(u => u.Id, u => BuildName(u.DisplayName, u.FirstName, u.LastName, u.DefaultEmail, u.Id));

        return rows.Select(r => new WikiPageListItem
        {
            Id = r.Id,
            Title = r.Title,
            SectionId = r.SectionId,
            SectionName = r.SectionName,
            SectionIcon = r.SectionIcon,
            ModifiedDate = r.ModifiedDate,
            ModifiedByName = names.GetValueOrDefault(r.ModifiedByUserId),
            ReviewDueDate = r.ReviewDueDate,
            OwnerName = r.OwnerUserId.HasValue ? names.GetValueOrDefault(r.OwnerUserId.Value) : null,
        }).ToList();
    }

    /// <summary>Уровень страницы в дереве: корневая = 1.</summary>
    internal static int Depth(int pageId, IReadOnlyDictionary<int, int?> parents)
    {
        var depth = 1;
        var cursor = parents.GetValueOrDefault(pageId);
        var guard = 0;
        while (cursor.HasValue && guard++ < 100)
        {
            depth++;
            cursor = parents.GetValueOrDefault(cursor.Value);
        }
        return depth;
    }

    /// <summary>Высота поддерева: сама страница без детей = 1.</summary>
    internal static int SubtreeHeight(int pageId, IReadOnlyList<(int Id, int? ParentId)> nodes)
    {
        var children = nodes.Where(n => n.ParentId == pageId).Select(n => n.Id).ToList();
        return children.Count == 0 ? 1 : 1 + children.Max(c => SubtreeHeight(c, nodes));
    }

    private static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");

    private static string Clip(string? value, int max)
    {
        value = (value ?? string.Empty).Trim();
        return value.Length > max ? value[..max] : value;
    }

    internal static string BuildName(string? displayName, string? firstName, string? lastName, string? email, int id)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim();
        var full = $"{firstName} {lastName}".Trim();
        if (full.Length > 0)
            return full;
        return string.IsNullOrWhiteSpace(email) ? $"Пользователь #{id}" : email.Trim();
    }
}
