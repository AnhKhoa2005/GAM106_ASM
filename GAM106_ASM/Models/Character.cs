using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class Character
{
    public int CharacterId { get; set; }

    public int PlayerId { get; set; }

    public string CharacterName { get; set; } = null!;

    public virtual Player Player { get; set; } = null!;
}
