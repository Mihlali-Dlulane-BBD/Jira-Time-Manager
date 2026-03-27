using Jira_Time_Manager.Core.Models;

namespace Jira_Time_Manager.Core.Interface
{
    public interface IWorkLogParser
    {
        Task<IEnumerable<WorkLogImportDto>> ParseAsync(Stream fileStream);
    }
}
