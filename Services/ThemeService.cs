using Microsoft.JSInterop;
using Mero_Dainiki.Entities;

namespace Mero_Dainiki.Services
{
    // ThemeMode is defined in Mero_Dainiki.Entities


    public interface IThemeService
    {
        ThemeMode CurrentTheme { get; }
        Task InitializeThemeAsync();
        Task SetThemeAsync(ThemeMode theme);
        Task ToggleThemeAsync();
        event Action? OnThemeChanged;
    }

    public class ThemeService : IThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        
        public ThemeMode CurrentTheme { get; private set; } = ThemeMode.Light;
        public event Action? OnThemeChanged;

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeThemeAsync()
        {
            try
            {
                var themeStr = await _jsRuntime.InvokeAsync<string>("themeManager.getTheme");
                if (Enum.TryParse<ThemeMode>(themeStr, true, out var theme))
                {
                    CurrentTheme = theme;
                }
                else
                {
                    CurrentTheme = ThemeMode.Light;
                }
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing theme: {ex.Message}");
            }
        }

        public async Task SetThemeAsync(ThemeMode theme)
        {
            CurrentTheme = theme;
            
            // Map Enum to string expected by JS
            var themeStr = theme.ToString().ToLower();
            
            // Allow JS to handle the DOM update and persistence
            await _jsRuntime.InvokeVoidAsync("themeManager.setTheme", themeStr);
            
            NotifyStateChanged();
        }

        public async Task ToggleThemeAsync()
        {
            var newTheme = CurrentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
            await SetThemeAsync(newTheme);
        }

        private void NotifyStateChanged() => OnThemeChanged?.Invoke();
    }
}
