using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class Player
{
    public int PlayerId { get; set; }

    public string EmailAccount { get; set; } = null!;

    public string LoginPassword { get; set; } = null!;

    public int ExperiencePoints { get; set; }

    public int HealthBar { get; set; }

    public int FoodBar { get; set; }

    public string Role { get; set; } = null!;

    public virtual Character? Character { get; set; }

    public virtual ICollection<MonsterKill> MonsterKills { get; set; } = new List<MonsterKill>();

    public virtual ICollection<PlayHistory> PlayHistories { get; set; } = new List<PlayHistory>();

    public virtual ICollection<PlayerQuest> PlayerQuests { get; set; } = new List<PlayerQuest>();

    public virtual ICollection<ResourceGathering> ResourceGatherings { get; set; } = new List<ResourceGathering>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
