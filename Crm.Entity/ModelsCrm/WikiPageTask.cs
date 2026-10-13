using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>Связь страницы базы знаний с задачей (TaskCrm).</summary>
public partial class WikiPageTask
{
    public int Id { get; set; }

    public int PageId { get; set; }

    public int TaskId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }
}
