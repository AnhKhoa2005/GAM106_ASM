using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class Quest
{
    public int QuestId { get; set; }

    public string QuestName { get; set; } = null!;

    public string? Description { get; set; }

    public int ExperienceReward { get; set; }

    public virtual ICollection<PlayerQuest> PlayerQuests { get; set; } = new List<PlayerQuest>();
}
