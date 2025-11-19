using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Data;

var builder = WebApplication.CreateBuilder(args);

n// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

nvar connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=journalai;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

nvar app = builder.Build();

nif (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

napp.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

napp.Run();
