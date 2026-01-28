using Mero_Dainiki.Common;
using Mero_Dainiki.Data;
using Mero_Dainiki.Entities;
using Mero_Dainiki.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Mero_Dainiki.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<User>> LoginAsync(UserAuthModel model);
        Task<ServiceResult<User>> RegisterAsync(UserRegisterModel model);
        Task<bool> IsAuthenticatedAsync();
        Task<int> ValidateUserAsync(IJSRuntime js);
        Task LogoutAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private const string CurrentUserKey = "current_user_id";
        private const string AuthExpirationKey = "auth_expiration_date";

        public AuthService(AppDbContext context) => _context = context;

        public async Task<ServiceResult<User>> LoginAsync(UserAuthModel model)
        {
            try {
                var hash = SecurityUtils.HashString(model.Password);
                var user = await _context.Users.FirstOrDefaultAsync(u => (u.Username == model.Username || u.Email == model.Username) && u.PasswordHash == hash);

                if (user == null) return ServiceResult<User>.Fail("Invalid username/email or password.");
                if (!user.IsActive) return ServiceResult<User>.Fail("Account is inactive.");

                user.LastLoginAt = DateTime.UtcNow;
                _context.LoginHistories.Add(new LoginHistory { UserId = user.Id, LoginTime = DateTime.UtcNow, IsSuccessful = true });
                await _context.SaveChangesAsync();

                SetSession(user.Id);
                return ServiceResult<User>.Ok(user);
            }
            catch (Exception ex) { return ServiceResult<User>.Fail($"Login failed: {ex.Message}"); }
        }

        public async Task<ServiceResult<User>> RegisterAsync(UserRegisterModel model)
        {
            try {
                if (model.Password != model.ConfirmPassword) return ServiceResult<User>.Fail("Passwords do not match.");
                if (await _context.Users.AnyAsync(u => u.Username == model.Username)) return ServiceResult<User>.Fail("Username taken.");
                if (await _context.Users.AnyAsync(u => u.Email == model.Email)) return ServiceResult<User>.Fail("Email taken.");

                var user = new User { Username = model.Username, Email = model.Email, PasswordHash = SecurityUtils.HashString(model.Password), Pin = !string.IsNullOrEmpty(model.Pin) ? SecurityUtils.HashString(model.Pin) : null, CreatedAt = DateTime.UtcNow, IsActive = true };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _context.LoginHistories.Add(new LoginHistory { UserId = user.Id, LoginTime = DateTime.UtcNow, IsSuccessful = true });
                await _context.SaveChangesAsync();

                SetSession(user.Id);
                return ServiceResult<User>.Ok(user);
            }
            catch (Exception ex) { return ServiceResult<User>.Fail($"Registration failed: {ex.Message}"); }
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            var userId = Preferences.Default.Get(CurrentUserKey, 0);
            var exp = Preferences.Default.Get(AuthExpirationKey, DateTime.MinValue);
            if (userId > 0 && exp < DateTime.UtcNow) { LogoutAsync(); return Task.FromResult(false); }
            return Task.FromResult(userId > 0);
        }

        public async Task<int> ValidateUserAsync(IJSRuntime js)
        {
            try {
                var userId = Preferences.Default.Get(CurrentUserKey, 0);
                var exp = Preferences.Default.Get(AuthExpirationKey, DateTime.MinValue);
                if (userId > 0 && exp > DateTime.UtcNow) return userId;

                var lsUserId = await js.InvokeAsync<string>("localStorage.getItem", "userId");
                var lsExpStr = await js.InvokeAsync<string>("localStorage.getItem", "authExpiration");
                if (int.TryParse(lsUserId, out userId) && DateTime.TryParse(lsExpStr, out var lsExp) && lsExp > DateTime.UtcNow) {
                    SetSession(userId, lsExp);
                    return userId;
                }
                if (userId > 0) await LogoutAsync();
                return 0;
            } catch { return 0; }
        }

        public Task LogoutAsync() { Preferences.Default.Remove(CurrentUserKey); Preferences.Default.Remove(AuthExpirationKey); return Task.CompletedTask; }

        private void SetSession(int id, DateTime? exp = null) {
            Preferences.Default.Set(CurrentUserKey, id);
            Preferences.Default.Set(AuthExpirationKey, exp ?? DateTime.UtcNow.AddDays(7));
        }
    }
}
