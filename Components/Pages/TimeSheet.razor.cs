using Jira_Time_Manager.Core.Interface;
using Jira_Time_Manager.Core.Models;
using Jira_Time_Manager.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Jira_Time_Manager.Components.Pages
{
    public partial class TimeSheet
    {
        // ==========================================
        // DEPENDENCY INJECTION
        // ==========================================

        [Inject] public IWorkLogHandler Db { get; set; } = default!;
        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] public IWorkLogExportService ExportService { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;


        // ==========================================
        // STATE & DATA VARIABLES
        // ==========================================

        // --- Core Entities ---
        private Employee? LoggedInEmployee;
        private Employee? SelectedEmployee;
        private List<Employee> AllEmployees = new();
        private List<Project> AvailableProjects = new();

        // --- Data Collections ---
        private List<WorkLog> MyLogs = new();
        private List<WorkLog> PendingApprovals = new();
        private HashSet<WorkLog> DirtyLogs = new();

        // --- View State ---
        private bool IsManagerView = false;
        private HashSet<DateOnly> CollapsedDays = new();
        private DateOnly SelectedDate = DateOnly.FromDateTime(DateTime.Today);
        private DateOnly CurrentWeekStart;
        private readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

        // --- Export State ---
        private int ExportMonth { get; set; } = DateTime.Today.Month;
        private int ExportYear { get; set; } = DateTime.Today.Year;
        private int ExportEmployeeId { get; set; } = 0; // 0 = "All Employees"
        private bool IsExporting = false;


        // ==========================================
        // COMPUTED PROPERTIES (UI HELPERS)
        // ==========================================

        private bool CanGoNextWeek => CurrentWeekStart.AddDays(7) <= Today;

        private IEnumerable<WorkLog> CurrentWeekLogs =>
            MyLogs.Where(l => l.LogDate >= CurrentWeekStart && l.LogDate < CurrentWeekStart.AddDays(7));

        private IEnumerable<IGrouping<DateOnly, WorkLog>> GroupedMyLogs =>
            CurrentWeekLogs.GroupBy(w => w.LogDate).OrderByDescending(g => g.Key);


        // ==========================================
        // LIFECYCLE METHODS
        // ==========================================

        protected override async Task OnInitializedAsync()
        {
            await LoadInitialDataAsync();
            await InitializeAuthenticationAsync();
            SetInitialWeekStart();
            await SwitchToEmployeeView();
        }

        private async Task LoadInitialDataAsync()
        {
            var projects = await Db.GetAllProjectsAsync();
            AvailableProjects = projects.ToList();

            var employees = await Db.GetAllEmployeesAsync();
            AllEmployees = employees.OrderBy(e => e.FirstName).ToList();
        }

        private void SetInitialWeekStart()
        {
            var today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            CurrentWeekStart = DateOnly.FromDateTime(today.AddDays(-diff));
        }

        private async Task InitializeAuthenticationAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is { IsAuthenticated: true })
            {
                var loggedInStaffNo = user.FindFirst("StaffNo")?.Value;
                LoggedInEmployee = AllEmployees.FirstOrDefault(e => e.StaffNo == loggedInStaffNo);
                IsManagerView = user.IsInRole("Manager");
            }
        }


        // ==========================================
        // VIEW TOGGLES & NAVIGATION
        // ==========================================

        private async Task SwitchToEmployeeView()
        {
            IsManagerView = false;
            SelectedEmployee = LoggedInEmployee;
            await LoadLogsForSelectedEmployeeAsync();
        }

        private async Task SwitchToManagerView()
        {
            IsManagerView = true;
            SelectedEmployee = null;
            MyLogs.Clear();

            if (LoggedInEmployee != null)
            {
                var pending = await Db.GetPendingLogsForManagerAsync(LoggedInEmployee.EmployeeId);
                PendingApprovals = pending.ToList();
            }
        }

        private async Task OnManagerEmployeeSelected(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int empId) && empId > 0)
            {
                SelectedEmployee = AllEmployees.FirstOrDefault(emp => emp.EmployeeId == empId);
                await LoadLogsForSelectedEmployeeAsync();

                // Auto-jump to their most recent log date
                if (MyLogs.Any())
                {
                    var latestLogDate = MyLogs.Max(l => l.LogDate);
                    int diff = (7 + ((int)latestLogDate.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
                    CurrentWeekStart = latestLogDate.AddDays(-diff);
                    SelectedDate = latestLogDate;
                }
            }
            else
            {
                SelectedEmployee = null;
                MyLogs.Clear();
            }
        }

        private async Task LoadLogsForSelectedEmployeeAsync()
        {
            if (SelectedEmployee != null)
            {
                var logs = await Db.GetLogsForEmployeeAsync(SelectedEmployee.EmployeeId);
                MyLogs = logs.ToList();
            }
        }


        // ==========================================
        // DATE SELECTION HANDLERS
        // ==========================================

        private void PreviousWeek() => CurrentWeekStart = CurrentWeekStart.AddDays(-7);

        private void NextWeek()
        {
            if (CanGoNextWeek) CurrentWeekStart = CurrentWeekStart.AddDays(7);
        }

        private void SelectDay(DateOnly day)
        {
            if (day <= Today) SelectedDate = day;
        }

        private void OnDatePicked(ChangeEventArgs e)
        {
            if (DateOnly.TryParse(e.Value?.ToString(), out var pickedDate) && pickedDate <= Today)
            {
                SelectedDate = pickedDate;
                int diff = (7 + ((int)pickedDate.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
                CurrentWeekStart = pickedDate.AddDays(-diff);
            }
        }

        private void ToggleDay(DateOnly date)
        {
            if (!CollapsedDays.Add(date))
            {
                CollapsedDays.Remove(date);
            }
        }


        // ==========================================
        // DATA MUTATION HANDLERS (SAVE/ADD/APPROVE)
        // ==========================================

        private void AddNewTaskForSelected() => AddNewTask(SelectedDate);

        private void AddNewTask(DateOnly date)
        {
            if (SelectedEmployee == null) return;

            var newLog = new WorkLog
            {
                EmployeeId = SelectedEmployee.EmployeeId,
                LogDate = date,
                IsApproved = false,
                Hours = 0
            };

            MyLogs.Insert(0, newLog);
            MarkAsDirty(newLog);
            CollapsedDays.Remove(date);
        }

        private void MarkAsDirty(WorkLog log)
        {
            DirtyLogs.Add(log);
        }

        private async Task SaveAllChanges()
        {
            if (!DirtyLogs.Any()) return;

            foreach (var log in DirtyLogs)
            {
                if (log.LogId == 0) await Db.AddLogAsync(log);
                else await Db.UpdateLogAsync(log);
            }

            DirtyLogs.Clear();
        }

        private async Task HandleToggleApproval(WorkLog log)
        {
            await Db.UpdateLogAsync(log);
        }




        // ==========================================
        // EXPORT HANDLERS
        // ==========================================

        private async Task HandleExport()
        {
            IsExporting = true;
            try
            {
                var fileBytes = await ExportService.GenerateExportAsync(ExportYear, ExportMonth, ExportEmployeeId);
                using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));

                string empName = ExportEmployeeId == 0 ? "All_Team" : AllEmployees.FirstOrDefault(e => e.EmployeeId == ExportEmployeeId)?.StaffNo ?? "Emp";
                string fileName = $"Timesheet_{ExportYear}_{ExportMonth:D2}_{empName}.xlsx";

                await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
            }
            finally
            {
                IsExporting = false;
            }
        }
    }
}