using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class AuditLog
{
    public long AuditId { get; set; }

    public int? ActorUserId { get; set; }

    public string ActionName { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string? EntityKey { get; set; }

    public string? Details { get; set; }

    public DateTime HappenedAt { get; set; }

    public virtual User? ActorUser { get; set; }
}
