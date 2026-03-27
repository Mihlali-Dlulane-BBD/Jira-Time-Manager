using Jira_Time_Manager.Core.Services;
using Microsoft.AspNetCore.Components;

namespace Jira_Time_Manager.Components.Layout
{
    public partial class MainLayout
    {
        [Inject]
        public ThemeService Theme { get; set; } = default!;
        protected override void OnInitialized()
        {
            
            Theme.OnThemeChanged += StateHasChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Theme.InitializeAsync();
            }
        }

        public void Dispose()
        {
            Theme.OnThemeChanged -= StateHasChanged;
        }
    }
}
