using Jira_Time_Manager.Core.Data;
using Jira_Time_Manager.Core.Interface;
using Jira_Time_Manager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Jira_Time_Manager.Core.Services
{
    public class WorkLogHandler : IWorkLogHandler
    {
        private readonly IDbContextFactory<JiraTimeManagerDbContext> _dbFactory;

        public WorkLogHandler(IDbContextFactory<JiraTimeManagerDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<IEnumerable<Employee>> SearchEmployeesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return new List<Employee>();

            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Employees
                .Where(e => e.FirstName.Contains(searchTerm) || e.LastName.Contains(searchTerm))
                .Take(10)
                .ToListAsync();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int employeeId)
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Employees.FindAsync(employeeId);
        }

        public async Task<IEnumerable<WorkLog>> GetLogsForEmployeeAsync(int employeeId)
        {
            using var context = await _dbFactory.CreateDbContextAsync();

            return await context.WorkLogs
                .Include(w => w.Project)
                .Where(w => w.EmployeeId == employeeId)
                .OrderByDescending(w => w.LogDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<WorkLog>> GetPendingLogsForManagerAsync(int managerId)
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.WorkLogs
                .Include(w => w.Employee)
                .Include(w => w.Project)
                .Where(w => w.IsApproved == false && w.Employee.ManagerId == managerId)
                .OrderBy(w => w.LogDate)
                .ToListAsync();
        }

        public async Task<WorkLog?> GetLogByIdAsync(int logId)
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.WorkLogs.FindAsync(logId);
        }

        public async Task AddLogAsync(WorkLog log)
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            context.WorkLogs.Add(log);
            await context.SaveChangesAsync();
        }

        public async Task UpdateLogAsync(WorkLog log)
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            context.Entry(log).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        public async Task ApproveLogAsync(int logId)
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var log = await context.WorkLogs.FindAsync(logId);
            if (log != null)
            {
                log.IsApproved = true;
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteLogAsync(int logId)
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            var log = await context.WorkLogs.FindAsync(logId);
            if (log != null)
            {
                context.WorkLogs.Remove(log);
                await context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Employees.OrderBy(e => e.FirstName).ToListAsync();
        }
    }
}
