using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int PlayerId { get; set; }

    public int? ItemSheetId { get; set; }

    public int? VehicleId { get; set; }

    public DateTime TransactionTime { get; set; }

    public int TransactionValue { get; set; }

    public string TransactionType { get; set; } = null!;

    public virtual ItemSalesSheet? ItemSheet { get; set; }

    public virtual Player Player { get; set; } = null!;

    public virtual Vehicle? Vehicle { get; set; }
}
