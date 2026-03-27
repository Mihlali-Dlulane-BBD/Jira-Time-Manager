using Jira_Time_Manager.Core.Interface;
using Jira_Time_Manager.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jira_Time_Manager.Components.Pages
{
    public partial class Home
    {
        [Inject] public IWorkLogHandler Db { get; set; } = default!;

        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        private bool IsManagerView = false;
        private Employee? LoggedInEmployee;

        // --- Employee Metrics ---
        private decimal WeeklyTotal = 0;
        private decimal WeeklyPending = 0;
        private decimal WeeklyApproved = 0;
        private Dictionary<string, decimal> ProjectBreakdown = new();
        private List<WorkLog> RecentLogs = new();

        // --- Manager Metrics ---
        private List<WorkLog> TeamPendingLogs = new();
        private decimal TeamPendingHours = 0;

        protected override async Task OnInitializedAsync()
        {

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is { IsAuthenticated: true })
            {

                var staffNo = user.FindFirst("StaffNo")?.Value;


                var allEmployees = await Db.GetAllEmployeesAsync();
                LoggedInEmployee = allEmployees.FirstOrDefault(e => e.StaffNo == staffNo);


                IsManagerView = user.IsInRole("Manager");


                if (IsManagerView)
                {
                    await SwitchToManagerView();
                }
                else
                {
                    await SwitchToEmployeeView();
                }
            }
        }

        private async Task SwitchToEmployeeView()
        {
            IsManagerView = false;
            if (LoggedInEmployee == null) return;

            var allLogs = await Db.GetLogsForEmployeeAsync(LoggedInEmployee.EmployeeId);
            RecentLogs = allLogs.ToList();

            var today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = DateOnly.FromDateTime(today.AddDays(-diff));

            var thisWeekLogs = allLogs.Where(l => l.LogDate >= weekStart).ToList();

            WeeklyTotal = thisWeekLogs.Sum(l => l.Hours);
            WeeklyPending = thisWeekLogs.Where(l => !l.IsApproved).Sum(l => l.Hours);
            WeeklyApproved = thisWeekLogs.Where(l => l.IsApproved).Sum(l => l.Hours);

            ProjectBreakdown = thisWeekLogs
                .Where(l => l.Project != null)
                .GroupBy(l => l.Project!.ProjectName)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Hours))
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private async Task SwitchToManagerView()
        {
            IsManagerView = true;
            if (LoggedInEmployee == null) return;

            var pending = await Db.GetPendingLogsForManagerAsync(LoggedInEmployee.EmployeeId);
            TeamPendingLogs = pending.ToList();
            TeamPendingHours = TeamPendingLogs.Sum(l => l.Hours);
        }
    }
}