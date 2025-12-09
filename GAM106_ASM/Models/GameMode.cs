using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class GameMode
{
    public int ModeId { get; set; }

    public string ModeName { get; set; } = null!;

    public virtual ICollection<PlayHistory> PlayHistories { get; set; } = new List<PlayHistory>();
}
