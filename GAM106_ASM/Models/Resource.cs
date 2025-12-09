using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class Resource
{
    public int ResourceId { get; set; }

    public string ResourceName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<ResourceGathering> ResourceGatherings { get; set; } = new List<ResourceGathering>();
}
