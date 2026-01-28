using Mero_Dainiki.Data;
using Mero_Dainiki.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mero_Dainiki.Services
{
    public interface IUserService
    {
        Task<User?> GetCurrentUserAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<bool> UpdateUserAsync(User user);
    }

    public class UserService : BaseService, IUserService
    {
        public UserService(AppDbContext context) : base(context) { }

        public async Task<User?> GetCurrentUserAsync() => 
            IsUserAuthenticated ? await _context.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId && u.IsActive) : null;

        public async Task<User?> GetUserByIdAsync(int userId) => 
            await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        public async Task<bool> UpdateUserAsync(User user)
        {
            try { 
                _context.Users.Update(user); 
                await _context.SaveChangesAsync(); 
                return true; 
            } catch { return false; }
        }
    }
}
