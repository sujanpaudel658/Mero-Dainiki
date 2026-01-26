using Mero_Dainiki.Common;
using Mero_Dainiki.Data;
using Mero_Dainiki.Entities;
using Mero_Dainiki.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Security.Cryptography;
using System.Text;

namespace Mero_Dainiki.Services
{
    /// <summary>
    /// Interface for authentication operations
    /// </summary>
    public interface IAuthService
    {
        Task<ServiceResult<User>> LoginAsync(UserAuthModel model);
        Task<ServiceResult<User>> RegisterAsync(UserRegisterModel model);
        Task<bool> IsAuthenticatedAsync();
        Task<int> ValidateUserAsync(IJSRuntime js);
        Task LogoutAsync();
    }

    /// <summary>
    /// Authentication service implementation
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private const string CurrentUserKey = "current_user_id";

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<User>> LoginAsync(UserAuthModel model)
        {
            try
            {
                var passwordHash = SecurityUtils.HashString(model.Password);
                
                // Support both username and email login
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => (u.Username == model.Username || u.Email == model.Username) && u.PasswordHash == passwordHash);

                if (user == null)
                {
                    return ServiceResult<User>.Fail("Invalid username/email or password.");
                }

                if (!user.IsActive)
                {
                    return ServiceResult<User>.Fail("Account is inactive.");
                }

                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                
                // Log this login in LoginHistory for audit trail
                var loginHistory = new LoginHistory
                {
                    UserId = user.Id,
                    LoginTime = DateTime.UtcNow,
                    IsSuccessful = true
                };
                
                _context.LoginHistories.Add(loginHistory);
                await _context.SaveChangesAsync();

                // Store user ID in MAUI Preferences
                Preferences.Default.Set(CurrentUserKey, user.Id);
                return ServiceResult<User>.Ok(user);
            }
            catch (Exception ex)
            {
                return ServiceResult<User>.Fail($"Login failed: {ex.Message}");
            }
        }

        public async Task<ServiceResult<User>> RegisterAsync(UserRegisterModel model)
        {
            try
            {
                if (model.Password != model.ConfirmPassword)
                {
                    return ServiceResult<User>.Fail("Passwords do not match.");
                }

                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    return ServiceResult<User>.Fail("Username is already taken.");
                }

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    return ServiceResult<User>.Fail("Email is already registered.");
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = SecurityUtils.HashString(model.Password),
                    Pin = !string.IsNullOrEmpty(model.Pin) ? SecurityUtils.HashString(model.Pin) : null,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Log registration as first login
                var loginHistory = new LoginHistory
                {
                    UserId = user.Id,
                    LoginTime = DateTime.UtcNow,
                    IsSuccessful = true
                };
                _context.LoginHistories.Add(loginHistory);
                await _context.SaveChangesAsync();

                Preferences.Default.Set(CurrentUserKey, user.Id);
                return ServiceResult<User>.Ok(user);
            }
            catch (Exception ex)
            {
                return ServiceResult<User>.Fail($"Registration failed: {ex.Message}");
            }
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            var userId = Preferences.Default.Get(CurrentUserKey, 0);
            return Task.FromResult(userId > 0);
        }

        public async Task<int> ValidateUserAsync(IJSRuntime js)
        {
            try
            {
                var userId = Preferences.Default.Get(CurrentUserKey, 0);
                
                // If not in preferences, try localStorage
                if (userId <= 0)
                {
                    var lsUserId = await js.InvokeAsync<string>("localStorage.getItem", "userId");
                    if (!string.IsNullOrEmpty(lsUserId) && int.TryParse(lsUserId, out userId))
                    {
                        Preferences.Default.Set(CurrentUserKey, userId);
                    }
                }
                
                return userId;
            }
            catch
            {
                return 0;
            }
        }

        public Task LogoutAsync()
        {
            Preferences.Default.Remove(CurrentUserKey);
            return Task.CompletedTask;
        }
    }
}


