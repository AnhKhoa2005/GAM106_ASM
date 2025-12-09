using System;
using System.Collections.Generic;

namespace GAM106_ASM.Models;

public partial class Vehicle
{
    public int VehicleId { get; set; }

    public string VehicleName { get; set; } = null!;

    public string? Description { get; set; }

    public int PurchaseValue { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
