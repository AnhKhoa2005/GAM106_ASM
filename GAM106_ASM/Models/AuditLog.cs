using System;

namespace GAM106_ASM.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty; // Create, Update, Delete
        public string EntityName { get; set; } = string.Empty; // Player, Item, ...
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
    }
}