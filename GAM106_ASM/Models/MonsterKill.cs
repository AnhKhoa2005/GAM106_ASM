using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class MonsterKill
{
    public int PlayerId { get; set; }

    public int MonsterId { get; set; }

    public DateTime KillTime { get; set; }

    public int Quantity { get; set; }

    public virtual Monster Monster { get; set; } = null!;

    public virtual Player Player { get; set; } = null!;
}
