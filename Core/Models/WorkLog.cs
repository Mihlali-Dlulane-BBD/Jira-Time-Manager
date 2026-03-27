namespace Jira_Time_Manager.Core.Models;

public partial class WorkLog
{
    public int LogId { get; set; }

    public int EmployeeId { get; set; }

    public int ProjectId { get; set; }

    public string Description { get; set; } = null!;

    public string WorkCode { get; set; } = null!;

    public string? Comment { get; set; }

    public string? ReferenceNumber { get; set; }

    public DateOnly LogDate { get; set; }

    public decimal Hours { get; set; }

    public bool IsApproved { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}
