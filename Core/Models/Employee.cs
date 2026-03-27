using System;
using System.Collections.Generic;

namespace Jira_Time_Manager.Core.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public string StaffNo { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public int TeamId { get; set; }

    public int? ManagerId { get; set; }

    public virtual Manager? Manager { get; set; }

    public virtual Manager? ManagerNavigation { get; set; }

    public virtual Team Team { get; set; } = null!;

    public virtual ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
}
