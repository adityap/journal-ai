using JournalAI.Backend.Models;
using BCrypt.Net;

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

            // Hash a default dev password (devpassword) for local development using BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("devpassword");

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
