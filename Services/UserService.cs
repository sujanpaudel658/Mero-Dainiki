using Mero_Dainiki.Data;
using Mero_Dainiki.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mero_Dainiki.Services
{
    /// <summary>
    /// Interface for user data operations
    /// </summary>
    public interface IUserService
    {
        Task<User?> GetCurrentUserAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<bool> UpdateUserAsync(User user);
    }

    /// <summary>
    /// User service implementation for database operations
    /// </summary>
    public class UserService : BaseService, IUserService
    {
        public UserService(AppDbContext context) : base(context) { }

        /// <summary>
        /// Get the current logged-in user from database
        /// </summary>
        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                if (!IsUserAuthenticated) return null;
                return await _context.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId && u.IsActive);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get user by ID from database
        /// </summary>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Update user information in database
        /// </summary>
        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

