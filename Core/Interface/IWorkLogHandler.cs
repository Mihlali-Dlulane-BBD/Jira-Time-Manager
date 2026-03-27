using Jira_Time_Manager.Core.Models;

namespace Jira_Time_Manager.Core.Interface
{
    public interface IWorkLogHandler
    {

        Task<IEnumerable<WorkLog>> GetLogsForEmployeeAsync(int employeeId);
        Task<IEnumerable<WorkLog>> GetPendingLogsForManagerAsync(int managerId);

        Task<Employee?> GetEmployeeByIdAsync(int employeeId);
        Task<WorkLog?> GetLogByIdAsync(int logId);
        Task AddLogAsync(WorkLog log);
        Task UpdateLogAsync(WorkLog log);
        Task ApproveLogAsync(int logId);
        Task DeleteLogAsync(int logId);

        Task<IEnumerable<Employee>> SearchEmployeesAsync(string searchTerm);
        Task<IEnumerable<Project>> GetAllProjectsAsync();
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
    }
}
