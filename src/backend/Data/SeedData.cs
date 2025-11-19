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
                PasswordHash = "dev-placeholder-hash",
                Timezone = "UTC",
                NoTraining = true
            };
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
