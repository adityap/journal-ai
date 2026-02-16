using Microsoft.EntityFrameworkCore;
using JournalAI.Backend.Data;
using JournalAI.Backend.Services;
using Amazon.S3;
using Amazon;
using Hangfire;
using Hangfire.InMemory;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add AWS S3 Client
var isDev = builder.Environment.IsDevelopment();
if (isDev)
{
    // Use MinIO for local development
    var minioEndpoint = builder.Configuration["S3:Endpoint"] ?? "http://localhost:9000";
    var minioAccessKey = builder.Configuration["S3:AccessKey"] ?? "minioadmin";
    var minioSecretKey = builder.Configuration["S3:SecretKey"] ?? "minioadmin";

    var s3Config = new AmazonS3Config
    {
        ServiceURL = minioEndpoint,
        ForcePathStyle = true
    };

    builder.Services.AddSingleton<IAmazonS3>(
        new AmazonS3Client(minioAccessKey, minioSecretKey, s3Config)
    );
}
else
{
    // Use AWS S3 for production
    var awsAccessKey = builder.Configuration["AWS:AccessKey"] ?? "";
    var awsSecretKey = builder.Configuration["AWS:SecretKey"] ?? "";
    var awsRegion = builder.Configuration["AWS:Region"] ?? "us-east-1";

    var s3Config = new AmazonS3Config
    {
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion)
    };

    builder.Services.AddSingleton<IAmazonS3>(
        new AmazonS3Client(awsAccessKey, awsSecretKey, s3Config)
    );
}

// Add services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EntryService>();
builder.Services.AddScoped<S3Service>();
builder.Services.AddScoped<MediaService>();
builder.Services.AddScoped<JobSchedulerService>();

// Add Hangfire for background job processing
if (isDev)
{
    // Use in-memory storage for development
    builder.Services.AddHangfire(config => config.UseInMemoryStorage());
}
else
{
    // Use SQL Server storage for production
    var hangfireConnection = builder.Configuration.GetConnectionString("HangfireConnection") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=journalai;Username=postgres;Password=postgres";
    
    builder.Services.AddHangfire(config => config
        .UseSqlServerStorage(hangfireConnection)
        .WithJobExpirationTimeout(TimeSpan.FromDays(7))
    );
}

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount;
    options.SchedulePollingInterval = TimeSpan.FromSeconds(5);
});

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

// Configure Hangfire dashboard (production only for security)
if (!app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "JournalAI Background Jobs"
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
