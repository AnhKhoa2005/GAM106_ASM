using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class PlayHistory
{
    public int PlayerId { get; set; }

    public int ModeId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public virtual GameMode Mode { get; set; } = null!;

    public virtual Player Player { get; set; } = null!;
}
