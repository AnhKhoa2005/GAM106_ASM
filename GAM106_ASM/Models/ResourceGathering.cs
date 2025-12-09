using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class ResourceGathering
{
    public int PlayerId { get; set; }

    public int ResourceId { get; set; }

    public DateTime GatheringTime { get; set; }

    public int Quantity { get; set; }

    public virtual Player Player { get; set; } = null!;

    public virtual Resource Resource { get; set; } = null!;
}
