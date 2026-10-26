using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class Permission
{
    public int PermissionId { get; set; }

    public string FunctionCode { get; set; } = null!;

    public string FunctionName { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
