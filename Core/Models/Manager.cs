using System;
using System.Collections.Generic;

namespace Jira_Time_Manager.Core.Models;

public partial class Manager
{
    public int ManagerId { get; set; }

    public int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
