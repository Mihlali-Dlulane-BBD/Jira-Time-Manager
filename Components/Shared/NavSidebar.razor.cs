using Jira_Time_Manager.Core.Services.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jira_Time_Manager.Components.Shared
{
    public partial class NavSidebar
    {
        [Inject]
        public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject]
        public NavigationManager NavManager { get; set; } = default!;

        private async Task HandleLogout()
        {  
            var customAuthStateProvider = (CustomAuthStateProvider)AuthStateProvider;
        
            await customAuthStateProvider.MarkUserAsLoggedOut();

            NavManager.NavigateTo("/login");
        }
    }
}
