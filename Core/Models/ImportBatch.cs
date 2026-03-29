using System;
using System.Collections.Generic;

namespace Jira_Time_Manager.Core.Models;

public partial class ImportBatch
{
    public int ImportBatchId { get; set; }

    public string FileName { get; set; } = null!;

    public DateTime ImportDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
}
