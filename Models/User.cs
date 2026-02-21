namespace DEEPFAKE.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsEmailVerified { get; set; }

        public string? OTPCode { get; set; }

        public DateTime? OTPGeneratedAt { get; set; }
    }
}
