using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>
/// Назначенный редактор базы знаний. SectionId = NULL — редактор всей базы знаний компании,
/// иначе — только указанного раздела. Руководители компании (владелец/Admin) редакторы всегда.
/// </summary>
public partial class WikiEditor
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int? SectionId { get; set; }

    public int UserId { get; set; }

    public int GrantedByUserId { get; set; }

    public DateTime GrantedDate { get; set; }
}
