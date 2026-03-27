using Jira_Time_Manager.Core.Models;

namespace Jira_Time_Manager.Core.Interface
{
    public interface IDataImportService
    {
        Task ImportWorkLogsAsync(IEnumerable<WorkLogImportDto> rawLogs);
    }
}
