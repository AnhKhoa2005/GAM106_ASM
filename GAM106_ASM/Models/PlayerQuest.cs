using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class PlayerQuest
{
    public int PlayerId { get; set; }

    public int QuestId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CompletionTime { get; set; }

    public virtual Player Player { get; set; } = null!;

    public virtual Quest Quest { get; set; } = null!;
}
