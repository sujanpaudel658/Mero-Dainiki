namespace Mero_Dainiki.Models
{
    /// <summary>
    /// Model for user authentication
    /// </summary>
    public class UserAuthModel
    {
        public string Username { get; set; } = string.Empty;  // Can be username or email
        public string Password { get; set; } = string.Empty;
        public string? Pin { get; set; }
    }

    /// <summary>
    /// Model for user registration
    /// </summary>
    public class UserRegisterModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string? Pin { get; set; }
    }
}
