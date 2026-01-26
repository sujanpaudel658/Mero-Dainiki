namespace Mero_Dainiki.Entities
{
    /// <summary>
    /// LoginHistory entity - audit trail for every login event per user
    /// </summary>
    public class LoginHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Foreign key to User
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public bool IsSuccessful { get; set; } = true;
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
    }
}
