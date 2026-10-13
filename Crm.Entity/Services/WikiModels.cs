namespace Crm.Entity.Services;

/// <summary>Раздел с данными для списка (только доступные пользователю).</summary>
public sealed class WikiSectionInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Icon { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public string AccessMode { get; init; } = "All";
    public int PageCount { get; init; }
    public DateTime? LastModified { get; init; }
    public bool CanEdit { get; init; }
}

/// <summary>Узел дерева страниц (плоский список, дерево собирается по ParentId).</summary>
public sealed class WikiTreeItem
{
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public string Title { get; init; } = "";
    public int SortOrder { get; init; }
    public bool IsPinned { get; init; }
    public bool IsStale { get; init; }
}

/// <summary>Краткие сведения о странице для списков (избранное, закреплённые, недавние, на проверку).</summary>
public sealed class WikiPageListItem
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public int SectionId { get; init; }
    public string SectionName { get; init; } = "";
    public string? SectionIcon { get; init; }
    public DateTime ModifiedDate { get; init; }
    public string? ModifiedByName { get; init; }
    public DateTime? ReviewDueDate { get; init; }
    public string? OwnerName { get; init; }
}

public sealed class WikiVersionInfo
{
    public int VersionNumber { get; init; }
    public string Title { get; init; } = "";
    public string? Comment { get; init; }
    public int AuthorUserId { get; init; }
    public string? AuthorName { get; init; }
    public DateTime CreatedDate { get; init; }
}

public sealed class WikiAttachmentInfo
{
    public int Id { get; init; }
    public int CurrentVersionId { get; init; }
    public int VersionNumber { get; init; }
    public string FileName { get; init; } = "";
    public string ContentType { get; init; } = "";
    public long SizeBytes { get; init; }
    public DateTime UploadedDate { get; init; }
    public string? UploadedByName { get; init; }
    public List<WikiAttachmentVersionInfo> History { get; init; } = new();
}

public sealed class WikiAttachmentVersionInfo
{
    public int Id { get; init; }
    public int VersionNumber { get; init; }
    public string FileName { get; init; } = "";
    public long SizeBytes { get; init; }
    public DateTime UploadedDate { get; init; }
    public string? UploadedByName { get; init; }
}

/// <summary>Файл, который можно отдать пользователю (доступ уже проверен).</summary>
public sealed class WikiFileInfo
{
    public int VersionId { get; init; }
    public int CompanyId { get; init; }
    public string FileName { get; init; } = "";
    public string StoredName { get; init; } = "";
    public string ContentType { get; init; } = "";
    public long SizeBytes { get; init; }
}

public sealed class WikiSearchHit
{
    public int PageId { get; init; }
    public string Title { get; init; } = "";
    public int SectionId { get; init; }
    public string SectionName { get; init; } = "";
    public string? SectionIcon { get; init; }
    public string? ContentText { get; init; }
    public DateTime ModifiedDate { get; init; }
    public List<string> MatchedFiles { get; init; } = new();
    public bool TitleMatch { get; init; }
}

public sealed class WikiLinkedTask
{
    public int TaskId { get; init; }
    public string Name { get; init; } = "";
    public int? Status { get; init; }
    public string? StatusName { get; init; }
    public DateTime? Deadline { get; init; }
    public string? AssigneeName { get; init; }
}

public sealed class WikiUserInfo
{
    public int UserId { get; init; }
    public string Name { get; init; } = "";
    public string? Email { get; init; }
    public bool IsManager { get; init; }
}

public enum WikiSaveStatus
{
    Ok,
    NotFound,
    Forbidden,
    Conflict,
    Invalid
}

public sealed class WikiSaveResult
{
    public WikiSaveStatus Status { get; init; }
    public int Version { get; init; }
    public string? Message { get; init; }
    public string? ModifiedByName { get; init; }
    public DateTime? ModifiedDate { get; init; }

    public static WikiSaveResult Of(WikiSaveStatus status, string? message = null) => new() { Status = status, Message = message };
}
