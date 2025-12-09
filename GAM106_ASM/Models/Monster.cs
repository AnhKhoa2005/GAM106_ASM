using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class Monster
{
    public int MonsterId { get; set; }

    public string MonsterName { get; set; } = null!;

    public int ExperienceReward { get; set; }

    public virtual ICollection<MonsterKill> MonsterKills { get; set; } = new List<MonsterKill>();
}
