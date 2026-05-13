using Microsoft.AspNetCore.Identity;

namespace LifeOS.Core.Entities 
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = "Robert";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
