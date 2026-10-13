using System;

namespace Crm.Entity.ModelsCrm;

/// <summary>Личное избранное: страница, отмеченная пользователем звёздочкой.</summary>
public partial class WikiFavorite
{
    public int Id { get; set; }

    public int PageId { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedDate { get; set; }
}
