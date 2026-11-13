
using Microsoft.AspNetCore.Identity;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.System;

namespace Kredvanta.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // A user may optionally have a borrower profile.
        public Borrower? Borrower { get; set; }

        public ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();

        public ICollection<AuditLog> AuditLogs { get; set; }
            = new List<AuditLog>();
    }
}

