
using Kredvanta.Models.Common;
using Kredvanta.Models.Identity;

namespace Kredvanta.Models.System
{
    public class AuditLog : BaseEntity
    {
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public string? EntityId { get; set; }

        public string? Description { get; set; }

        // Can contain serialized before/after change details
        public string? ChangesJson { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    }
}
