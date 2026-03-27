using Jira_Time_Manager.Core.Services.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jira_Time_Manager.Components.Pages
{
    public partial class Login
    {
        [Inject]
        public AuthService AuthService { get; set; } = default!;

        [Inject]
        public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        private string LoginStaffNo { get; set; } = string.Empty;
        private string ErrorMessage { get; set; } = string.Empty;
        private bool IsLoading { get; set; } = false;

        private async Task HandleLogin()
        {
            ErrorMessage = string.Empty;
            IsLoading = true;

            try
            {
             
                var token = await AuthService.LoginAsync(LoginStaffNo);

                if (token != null)
                {
               
                    var customAuthStateProvider = (CustomAuthStateProvider)AuthStateProvider;

                  
                    await customAuthStateProvider.MarkUserAsAuthenticated(token);

                   
                    NavManager.NavigateTo("/");
                }
                else
                {
                    ErrorMessage = "Invalid Staff Number. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while trying to log in.";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
