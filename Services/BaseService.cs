using Mero_Dainiki.Data;

namespace Mero_Dainiki.Services
{
    /// <summary>
    /// Abstract base class for services that require database access and current user identification
    /// </summary>
    public abstract class BaseService
    {
        protected readonly AppDbContext _context;
        protected const string CurrentUserKey = "current_user_id";

        protected BaseService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves the current logged-in user ID from platform preferences
        /// </summary>
        protected int CurrentUserId => Preferences.Default.Get(CurrentUserKey, 0);

        /// <summary>
        /// Validates if a user is authenticated
        /// </summary>
        protected bool IsUserAuthenticated => CurrentUserId > 0;
    }
}
