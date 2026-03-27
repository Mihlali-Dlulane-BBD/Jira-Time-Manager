using Jira_Time_Manager.Core.Interface;
using Microsoft.JSInterop;

namespace Jira_Time_Manager.Core.Services
{
    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILocalStorageService _localStorage;

        public bool IsDarkMode { get; private set; } = true;

        public event Action? OnThemeChanged;

        public ThemeService(IJSRuntime jsRuntime, ILocalStorageService localStorage)
        {
            _jsRuntime = jsRuntime;
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var storedTheme = await _localStorage.GetItemAsync<string>("isDarkMode");
                if (storedTheme != null)
                {
                    IsDarkMode = storedTheme == "true";
                    OnThemeChanged?.Invoke();
                }
            }
            catch
            {
              
            }
        }

        public async Task ToggleThemeAsync()
        {
            IsDarkMode = !IsDarkMode;
            await _localStorage.SetItemAsync("isDarkMode", IsDarkMode.ToString().ToLower());
            OnThemeChanged?.Invoke();
        }
    }
}
