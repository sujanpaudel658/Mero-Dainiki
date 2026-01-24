using Mero_Dainiki.Common;
using Mero_Dainiki.Data;
using Mero_Dainiki.Entities;
using Mero_Dainiki.Models;
using Microsoft.EntityFrameworkCore;
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
        Task<ServiceResult> VerifyPinAsync(string pin);
        Task<ServiceResult> SetPinAsync(string pin);
        Task<bool> IsAuthenticatedAsync();
        Task LogoutAsync();
    }

    /// <summary>
    /// Authentication service implementation
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private const string CurrentUserKey = "current_user_id";
        private const string PinKey = "app_pin";

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<User>> LoginAsync(UserAuthModel model)
        {
            try
            {
                var passwordHash = HashPassword(model.Password);
                // Support both username and email login
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => (u.Username == model.Username || u.Email == model.Username) && u.PasswordHash == passwordHash);

                if (user == null)
                {
                    return ServiceResult<User>.Fail("Invalid username/email or password.");
                }

                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                Preferences.Default.Set(CurrentUserKey, user.Id);
                return ServiceResult<User>.Ok(user);
            }
            catch (Exception ex)
            {
                return ServiceResult<User>.Fail($"Login error: {ex.Message}");
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

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (existingUser != null)
                {
                    return ServiceResult<User>.Fail("Username already taken.");
                }

                var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingEmail != null)
                {
                    return ServiceResult<User>.Fail("Email already registered.");
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    Pin = model.Pin,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                Preferences.Default.Set(CurrentUserKey, user.Id);
                return ServiceResult<User>.Ok(user);
            }
            catch (Exception ex)
            {
                return ServiceResult<User>.Fail($"Registration error: {ex.Message}");
            }
        }

        public Task<ServiceResult> VerifyPinAsync(string pin)
        {
            var savedPin = Preferences.Default.Get(PinKey, string.Empty);
            if (string.IsNullOrEmpty(savedPin))
            {
                return Task.FromResult(ServiceResult.Ok());
            }

            return Task.FromResult(savedPin == pin
                ? ServiceResult.Ok()
                : ServiceResult.Fail("Invalid PIN."));
        }

        public Task<ServiceResult> SetPinAsync(string pin)
        {
            try
            {
                Preferences.Default.Set(PinKey, pin);
                return Task.FromResult(ServiceResult.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(ServiceResult.Fail($"Error setting PIN: {ex.Message}"));
            }
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            var userId = Preferences.Default.Get(CurrentUserKey, 0);
            return Task.FromResult(userId > 0);
        }

        public Task LogoutAsync()
        {
            Preferences.Default.Remove(CurrentUserKey);
            return Task.CompletedTask;
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
