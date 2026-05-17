using LifeOS.Core.Enums;

namespace LifeOS.Core.Entities
{
    public class XPTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;

        public XPSource Source { get; set; }
        public int XPAmount { get; set; }
        public Guid? SourceEntityId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }

        public ApplicationUser? User { get; set; }
    }
}