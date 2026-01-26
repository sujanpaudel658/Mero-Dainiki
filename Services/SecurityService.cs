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
        Task<ServiceResult<bool>> HasPinAsync();
        bool IsUnlocked { get; }
        void ResetUnlockState();
    }

    public class SecurityService : BaseService, ISecurityService
    {
        // Track unlock state per session - default is LOCKED (true means unlocked)
        private static bool _isSessionUnlocked = false;
        private static DateTime? _unlockTime = null;
        private const int UNLOCK_TIMEOUT_MINUTES = 30;
        
        // Public property to check if journal is currently unlocked
        public bool IsUnlocked => _isSessionUnlocked && !IsUnlockExpired();

        public SecurityService(AppDbContext context) : base(context) { }
        
        private bool IsUnlockExpired()
        {
            if (!_unlockTime.HasValue) return true;
            return (DateTime.UtcNow - _unlockTime.Value).TotalMinutes > UNLOCK_TIMEOUT_MINUTES;
        }

        public async Task<ServiceResult<bool>> HasPinAsync()
        {
            try
            {
                if (!IsUserAuthenticated) return ServiceResult<bool>.Ok(false);
                
                var user = await _context.Users.FindAsync(CurrentUserId);
                return ServiceResult<bool>.Ok(!string.IsNullOrEmpty(user?.Pin));
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> SetPinAsync(string pin)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
                    return ServiceResult<bool>.Fail("PIN must be at least 4 characters.");

                if (!IsUserAuthenticated) return ServiceResult<bool>.Fail("Unauthorized.");

                var user = await _context.Users.FindAsync(CurrentUserId);
                if (user == null) return ServiceResult<bool>.Fail("User not found.");

                user.Pin = SecurityUtils.HashString(pin);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> VerifyPinAsync(string pin)
        {
            try
            {
                if (!IsUserAuthenticated) return ServiceResult<bool>.Fail("Unauthorized.");

                var user = await _context.Users.FindAsync(CurrentUserId);
                if (user == null) return ServiceResult<bool>.Fail("User not found.");

                if (string.IsNullOrWhiteSpace(user.Pin)) return ServiceResult<bool>.Fail("No PIN set.");

                bool isValid = SecurityUtils.VerifyHash(pin, user.Pin);
                return isValid ? ServiceResult<bool>.Ok(true) : ServiceResult<bool>.Fail("Invalid PIN.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> ChangePinAsync(string oldPin, string newPin)
        {
            var verifyResult = await VerifyPinAsync(oldPin);
            if (!verifyResult.Success) return verifyResult;

            return await SetPinAsync(newPin);
        }

        public async Task<ServiceResult<bool>> RemovePinAsync(string pin)
        {
            try
            {
                var verifyResult = await VerifyPinAsync(pin);
                if (!verifyResult.Success) return verifyResult;

                var user = await _context.Users.FindAsync(CurrentUserId);
                if (user == null) return ServiceResult<bool>.Fail("User not found.");

                user.Pin = null;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                ResetUnlockState();
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> IsJournalLockedAsync()
        {
            try
            {
                if (!IsUserAuthenticated) return ServiceResult<bool>.Ok(false);

                var user = await _context.Users.FindAsync(CurrentUserId);
                if (user == null || string.IsNullOrWhiteSpace(user.Pin)) return ServiceResult<bool>.Ok(false);

                bool isLocked = !_isSessionUnlocked || IsUnlockExpired();
                return ServiceResult<bool>.Ok(isLocked);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UnlockJournalAsync(string pin)
        {
            try
            {
                var verifyResult = await VerifyPinAsync(pin);
                if (!verifyResult.Success) return verifyResult;

                _isSessionUnlocked = true;
                _unlockTime = DateTime.UtcNow;

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> LockJournalAsync()
        {
            try
            {
                if (!IsUserAuthenticated) return ServiceResult<bool>.Fail("Unauthorized.");

                var user = await _context.Users.FindAsync(CurrentUserId);
                if (user == null || string.IsNullOrWhiteSpace(user.Pin)) return ServiceResult<bool>.Fail("No PIN set.");

                ResetUnlockState();
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error: {ex.Message}");
            }
        }
        
        public void ResetUnlockState()
        {
            _isSessionUnlocked = false;
            _unlockTime = null;
        }
    }
}

