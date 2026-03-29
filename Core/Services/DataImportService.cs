using Jira_Time_Manager.Core.Data;
using Jira_Time_Manager.Core.Interface;
using Jira_Time_Manager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Jira_Time_Manager.Core.Services
{
    public class DataImportService : IDataImportService
    {
        private readonly IDbContextFactory<JiraTimeManagerDbContext> _dbFactory;
        private readonly bool _allowDuplicateLogs = false;

        public DataImportService(IDbContextFactory<JiraTimeManagerDbContext> dbFactory)
        {

            _dbFactory = dbFactory;
        }

        public async Task ImportWorkLogsAsync(IEnumerable<WorkLogImportDto> rawLogs, string fileName)
        {
            using var context = await _dbFactory.CreateDbContextAsync();

            var importBatch = new ImportBatch
            {
                FileName = fileName,
                ImportDate = DateTime.Now,
                Status = "Active"
            };

            context.ImportBatches.Add(importBatch);

            var defaultTeam = await GetOrCreateDefaultTeamAsync(context);

            foreach (var rawLog in rawLogs)
            {
                var client = await GetOrCreateClientAsync(context, rawLog.ClientName);
                var project = await GetOrCreateProjectAsync(context, rawLog.ProjectName, client.ClientId);
                var employee = await GetOrCreateEmployeeAsync(context, rawLog, defaultTeam.TeamId);

                var newLog = MapToWorkLogEntity(rawLog, employee.EmployeeId, project.ProjectId);
                newLog.ImportBatch = importBatch;

                if (!_allowDuplicateLogs)
                {
                    bool isDuplicate = await IsDuplicateLogAsync(context, newLog);
                    if (isDuplicate)
                    {
                        continue;
                    }
                }

                context.WorkLogs.Add(newLog);
            }


            await context.SaveChangesAsync();
        }

        private async Task<Team> GetOrCreateDefaultTeamAsync(JiraTimeManagerDbContext context)
        {
            var team = await context.Teams.FirstOrDefaultAsync(t => t.TeamName == "Unassigned");
            if (team == null)
            {
                team = new Team { TeamName = "Unassigned" };
                context.Teams.Add(team);
                await context.SaveChangesAsync();
            }
            return team;
        }

        private async Task<Client> GetOrCreateClientAsync(JiraTimeManagerDbContext context, string clientName)
        {
            var client = await context.Clients.FirstOrDefaultAsync(c => c.ClientName == clientName);
            if (client == null)
            {
                client = new Client { ClientName = clientName };
                context.Clients.Add(client);
                await context.SaveChangesAsync();
            }
            return client;
        }

        private async Task<Project> GetOrCreateProjectAsync(JiraTimeManagerDbContext context, string projectName, int clientId)
        {
            var project = await context.Projects.FirstOrDefaultAsync(p => p.ProjectName == projectName && p.ClientId == clientId);
            if (project == null)
            {
                project = new Project { ProjectName = projectName, ClientId = clientId };
                context.Projects.Add(project);
                await context.SaveChangesAsync();
            }
            return project;
        }

        private async Task<Employee> GetOrCreateEmployeeAsync(JiraTimeManagerDbContext context, WorkLogImportDto rawLog, int defaultTeamId)
        {
            var employee = await context.Employees.FirstOrDefaultAsync(e => e.StaffNo == rawLog.StaffNo);
            if (employee == null)
            {
                var nameParts = rawLog.StaffName.Split(' ', 2);
                employee = new Employee
                {
                    StaffNo = rawLog.StaffNo,
                    FirstName = nameParts[0],
                    LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                    TeamId = defaultTeamId
                };
                context.Employees.Add(employee);
                await context.SaveChangesAsync();
            }
            return employee;
        }

        private async Task<bool> IsDuplicateLogAsync(JiraTimeManagerDbContext context, WorkLog newLog)
        {
            return await context.WorkLogs.AnyAsync(w =>
                w.EmployeeId == newLog.EmployeeId &&
                w.ProjectId == newLog.ProjectId &&
                w.LogDate == newLog.LogDate &&
                w.Hours == newLog.Hours &&
                w.WorkCode == newLog.WorkCode);
        }

        private WorkLog MapToWorkLogEntity(WorkLogImportDto rawLog, int employeeId, int projectId)
        {
            return new WorkLog
            {
                EmployeeId = employeeId,
                ProjectId = projectId,
                Description = rawLog.Description,
                WorkCode = rawLog.WorkCode,
                Comment = rawLog.Comment,
                ReferenceNumber = rawLog.ReferenceNumber,
                LogDate = rawLog.Date,
                Hours = rawLog.Hours,
                IsApproved = false
            };
        }
    }
}
