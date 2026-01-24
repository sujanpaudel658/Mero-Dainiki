using Mero_Dainiki.Data;
using Mero_Dainiki.Entities;
using Mero_Dainiki.Common;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Mero_Dainiki.Services
{
    /// <summary>
    /// Security service for PIN protection and journal locking
    /// </summary>
    public interface ISecurityService
    {
        Task<ServiceResult<bool>> SetPinAsync(string pin);
        Task<ServiceResult<bool>> VerifyPinAsync(string pin);
        Task<ServiceResult<bool>> ChangePinAsync(string oldPin, string newPin);
        Task<ServiceResult<bool>> RemovePinAsync(string pin);
        Task<ServiceResult<bool>> IsJournalLockedAsync();
        Task<ServiceResult<bool>> UnlockJournalAsync(string pin);
        Task<ServiceResult<bool>> LockJournalAsync();
    }

    public class SecurityService : ISecurityService
    {
        private readonly AppDbContext _context;
        private bool _isJournalLocked = false;
        private DateTime? _lockTime = null;
        private const int LOCK_TIMEOUT_MINUTES = 30;
        private int _currentUserId = 1; 

        public SecurityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<bool>> SetPinAsync(string pin)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
                    return new ServiceResult<bool> { Success = false, ErrorMessage = "PIN must be at least 4 characters" };

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUserId);
                if (user == null)
                    return new ServiceResult<bool> { Success = false, ErrorMessage = "User not found" };

                user.Pin = HashPin(pin);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return new ServiceResult<bool> { Success = true, Data = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool> { Success = false, ErrorMessage = $"Error setting PIN: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<bool>> VerifyPinAsync(string pin)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUserId);
                if (user == null)
                    return new ServiceResult<bool> { Success = false, ErrorMessage = "User not found" };

                if (string.IsNullOrWhiteSpace(user.Pin))
                    return new ServiceResult<bool> { Success = false, ErrorMessage = "No PIN set" };

                var pinHash = HashPin(pin);
                bool isValid = user.Pin == pinHash;

                return new ServiceResult<bool> 
                { 
                    Success = isValid, 
                    Data = isValid, 
                    ErrorMessage = isValid ? null : "Invalid PIN" 
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool> { Success = false, ErrorMessage = $"Error verifying PIN: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<bool>> ChangePinAsync(string oldPin, string newPin)
        {
            try
            {
                var verifyResult = await VerifyPinAsync(oldPin);
                if (!verifyResult.Success)
                    return new ServiceResult<bool> { Success = false, ErrorMessage = "Current PIN is incorrect" };

                return await SetPinAsync(newPin);
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool> { Success = false, ErrorMessage = $"Error changing PIN: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<bool>> RemovePinAsync(string pin)
        {
            try
            {
                var verifyResult = await VerifyPinAsync(pin);
                if (!verifyResult.Success)
                    return new ServiceResult<bool> { Success = false, ErrorMessage = "PIN is incorrect" };

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUserId);
                if (user == null)
                    return new ServiceResult<bool> { Success = false, ErrorMessage = "User not found" };

                user.Pin = null;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _isJournalLocked = false;
                _lockTime = null;

                return new ServiceResult<bool> { Success = true, Data = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool> { Success = false, ErrorMessage = $"Error removing PIN: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<bool>> IsJournalLockedAsync()
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUserId);
                if (user == null || string.IsNullOrWhiteSpace(user.Pin))
                    return new ServiceResult<bool> { Success = true, Data = false };

                if (_isJournalLocked && _lockTime.HasValue)
                {
                    if ((DateTime.UtcNow - _lockTime.Value).TotalMinutes > LOCK_TIMEOUT_MINUTES)
                    {
                        _isJournalLocked = true;
                        _lockTime = DateTime.UtcNow;
                    }
                }

                return new ServiceResult<bool> { Success = true, Data = _isJournalLocked };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool> { Success = false, ErrorMessage = $"Error checking lock status: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<bool>> UnlockJournalAsync(string pin)
        {
            try
            {
                var verifyResult = await VerifyPinAsync(pin);
                if (!verifyResult.Success)
                    return new ServiceResult<bool> { Success = false, ErrorMessage = "Invalid PIN" };

                _isJournalLocked = false;
                _lockTime = null;

                return new ServiceResult<bool> { Success = true, Data = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool> { Success = false, ErrorMessage = $"Error unlocking journal: {ex.Message}" };
            }
        }

        public async Task<ServiceResult<bool>> LockJournalAsync()
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUserId);
                if (user == null || string.IsNullOrWhiteSpace(user.Pin))
                    return new ServiceResult<bool> { Success = false, ErrorMessage = "No PIN set" };

                _isJournalLocked = true;
                _lockTime = DateTime.UtcNow;

                return new ServiceResult<bool> { Success = true, Data = true };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool> { Success = false, ErrorMessage = $"Error locking journal: {ex.Message}" };
            }
        }

        private string HashPin(string pin)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pin));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
