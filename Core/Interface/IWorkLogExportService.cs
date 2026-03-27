namespace Jira_Time_Manager.Core.Interface
{
    public interface IWorkLogExportService
    {
        Task<byte[]> GenerateExportAsync(int year, int month, int? specificEmployeeId = null);
    }
}
