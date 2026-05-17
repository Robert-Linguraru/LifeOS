using LifeOS.Core.Entities;
using LifeOS.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LifeOS.Infrastructure.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<AppDbContext>();

            const string email = "robert@lifeos.com";
            const string password = "YourPassword1!";

            ApplicationUser? user = await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = "Robert"
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                    throw new Exception($"Seed failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Seed UserProgression if not exists
            var exists = await db.UserProgressions.AnyAsync(p => p.UserId == user.Id);
            if (!exists)
            {
                db.UserProgressions.Add(new UserProgression
                {
                    UserId = user.Id,
                    TotalLifetimeXP = 0,
                    CurrentLevel = 1,
                    CurrentEchelon = Echelon.Iron,
                    DailyQuestXPToday = 0,
                    DailyQuestXPDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });
                await db.SaveChangesAsync();
            }
        }
    }
}