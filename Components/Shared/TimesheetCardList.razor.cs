using Jira_Time_Manager.Core.Models;
using Microsoft.AspNetCore.Components;

namespace Jira_Time_Manager.Components.Shared
{
    public partial class TimesheetCardList
    {
        
        [Parameter, EditorRequired]
        public IEnumerable<IGrouping<DateOnly, WorkLog>> GroupedLogs { get; set; } = default!;

        [Parameter, EditorRequired]
        public List<Project> AvailableProjects { get; set; } = new();

        [Parameter, EditorRequired]
        public HashSet<DateOnly> CollapsedDays { get; set; } = new();

       
        [Parameter]
        public EventCallback<DateOnly> OnAddNewTask { get; set; }

        [Parameter]
        public EventCallback<DateOnly> OnToggleDay { get; set; }

        [Parameter]
        public EventCallback<WorkLog> OnLogModified { get; set; }

        [Parameter] public bool IsManagerView { get; set; } = false;
        [Parameter] public EventCallback<WorkLog> OnApproveLog { get; set; }
    }
}
