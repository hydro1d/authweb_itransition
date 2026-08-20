using System;
using System.ComponentModel.DataAnnotations;

namespace AuthWeb.Models
{
    // note: User entity representing authenticated accounts in the system.
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Status values: "Active", "Blocked", "Unverified"
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = UserStatus.Unverified;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public string? ConfirmationToken { get; set; }
    }

    public static class UserStatus
    {
        public const string Active = "Active";
        public const string Blocked = "Blocked";
        public const string Unverified = "Unverified";
    }
}
