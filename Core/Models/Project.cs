using System;
using System.Collections.Generic;

namespace Jira_Time_Manager.Core.Models;

public partial class Project
{
    public int ProjectId { get; set; }

    public int ClientId { get; set; }

    public string ProjectName { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
}
