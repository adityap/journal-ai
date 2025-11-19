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

// JWT Authentication
// NOTE: HS256 requires a sufficiently long symmetric key (>= 256 bits).
// Use configuration (environment variable Jwt__Key) in production. For local
// development we provide a long default so token signing does not throw.
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-local-key-please-change-to-a-secure-secret-with-at-least-32-bytes!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "journalai";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "journalai-users";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        // Ensure the signing key is at least 256 bits; if the provided key string is shorter
        // derive a 256-bit key by hashing with SHA256 so HS256 can operate.
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtKey).Length >= 32
                ? System.Text.Encoding.UTF8.GetBytes(jwtKey)
                : System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(jwtKey))
        )
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();

    // Ensure DB created and seeded in development
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        SeedData.EnsureSeedData(db);
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
