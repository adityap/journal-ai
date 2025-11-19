using JournalAI.Backend.Models;

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
                Username = "devuser",
                Timezone = "UTC",
                NoTraining = true
            };

            // Hash a default dev password (devpassword) for local development
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, "devpassword");

            db.Users.Add(user);

            var entry = new Entry
            {
                UserId = user.Id,
                Title = "Welcome",
                Body = "This is a seeded entry for local development.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsPrivate = false
            };
            db.Entries.Add(entry);
            db.SaveChanges();
        }
    }
}
