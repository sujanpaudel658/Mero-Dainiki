namespace Mero_Dainiki.Services
{
    public interface IThemeService
    {
        Task<string> GetThemeAsync();
        Task SaveThemeAsync(string theme);
    }

    public class ThemeService : IThemeService
    {
        private readonly string _preferencesKey = "app_theme";

        public Task<string> GetThemeAsync()
        {
            // Get theme from platform preferences (persisted storage)
            var theme = Preferences.Default.Get(_preferencesKey, "light");
            return Task.FromResult(theme);
        }

        public Task SaveThemeAsync(string theme)
        {
            // Save theme to platform preferences (persisted storage)
            Preferences.Default.Set(_preferencesKey, theme);
            return Task.CompletedTask;
        }
    }
}
