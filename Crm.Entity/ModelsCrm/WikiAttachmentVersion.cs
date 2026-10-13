using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Версия файла вложения. StoredName — случайное имя файла на диске (папка компании),
/// FileName — исходное имя файла (только для отображения и Content-Disposition).
/// </summary>
public partial class WikiAttachmentVersion
{
    public int Id { get; set; }

    public int AttachmentId { get; set; }

    public int VersionNumber { get; set; }

    public string FileName { get; set; } = null!;

    public string StoredName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public int UploadedByUserId { get; set; }

    public DateTime UploadedDate { get; set; }

    public virtual WikiAttachment Attachment { get; set; } = null!;
}
