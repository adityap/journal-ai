using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var isDev = builder.Environment.IsDevelopment();
if (isDev)
{
    // Use a local file SQLite DB for development so migrations and local runs don't need Postgres
    var sqliteConnection = builder.Configuration.GetConnectionString("SqliteDev") ?? "Data Source=journalai-dev.db";
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnection));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=journalai;Username=postgres;Password=postgres";
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString)
    );
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Ensure DB created and seeded in development
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        SeedData.EnsureSeedData(db);
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
