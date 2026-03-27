using Jira_Time_Manager.Core.Interface;

using Microsoft.JSInterop;
using System.Text.Json;

namespace Jira_Time_Manager.Core.Services
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            if (value is string stringValue)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, stringValue);
            }
            else
            {

                var json = JsonSerializer.Serialize(value);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
            }
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            var result = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

            if (string.IsNullOrWhiteSpace(result))
                return default;


            if (typeof(T) == typeof(string))
            {
                return (T)(object)result;
            }


            try
            {
                return JsonSerializer.Deserialize<T>(result);
            }
            catch (JsonException)
            {

                return default;
            }
        }

        public async Task RemoveItemAsync(string key)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
    }
}
