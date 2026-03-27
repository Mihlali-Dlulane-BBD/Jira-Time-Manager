namespace Jira_Time_Manager.Core.Models
{
    public class WorkLogImportDto
    {
        public string StaffNo { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string WorkCode { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateOnly Date { get; set; }
        public decimal Hours { get; set; }
    }
}
