using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class ItemType
{
    public int ItemTypeId { get; set; }

    public string ItemTypeName { get; set; } = null!;

    public virtual ICollection<ItemSalesSheet> ItemSalesSheets { get; set; } = new List<ItemSalesSheet>();
}
