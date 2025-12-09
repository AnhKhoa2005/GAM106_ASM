using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class ItemSalesSheet
{
    public int ItemSheetId { get; set; }

    public int ItemTypeId { get; set; }

    public string ItemVersionName { get; set; } = null!;

    public int PurchaseValue { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ItemType ItemType { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
