using System;
using System.Collections.Generic;

namespace Crm.Entity.ModelsCrm;

public partial class RefStatus
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? SortOrder { get; set; }
}
