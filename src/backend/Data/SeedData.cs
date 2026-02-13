using JournalAI.Backend.Models;
using Microsoft.AspNetCore.Identity;

namespace JournalAI.Backend.Data;

public static class SeedData
{
    public static void EnsureSeedData(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (!db.Users.Any())
        {
            var user = new User
            {
                Email = "dev@example.com",
                Timezone = "UTC",
                NoTrainingUse = true
            };

            // Hash a default dev password (devpassword) for local development
            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, "devpassword");

            db.Users.Add(user);

            var entry = new Entry
            {
                UserId = user.Id,
                Title = "Welcome",
                BodyText = "This is a seeded entry for local development.",
                CreatedAt = DateTime.UtcNow,
                ReadOnlyAfter = DateTime.UtcNow.AddDays(1),
                Type = "text",
                Confidentiality = "public"
            };
            db.Entries.Add(entry);
            db.SaveChanges();
        }
    }
}
