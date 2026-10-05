using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using dotenv.net;
using Npgsql;
using CampFix.Data;
using CampFix.Endpoints;
using CampFix.Hubs;

// 1. Load environment variables from .env file if present
DotEnv.Load(new DotEnvOptions(ignoreExceptions: true));

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization to format enums as readable strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Configure CORS for web client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

// 2. Database Provider Configuration (Neon PostgreSQL with SQLite Fallback)
var rawDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
               ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(rawDbUrl))
{
    var pgConnString = FormatPostgreSqlConnectionString(rawDbUrl);
    Console.WriteLine("[CampFix] -> Connecting to Neon Cloud PostgreSQL Database.");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(pgConnString));
}
else
{
    Console.WriteLine("[CampFix] -> No DATABASE_URL found. Falling back to local SQLite database (campfix.db).");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=campfix.db"));
}

// 3. Register SignalR & Swagger
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CampFix API",
        Version = "v1",
        Description = "Campus Facility Issue Tracker - DCIT 318 Semester Project API"
    });
});

var app = builder.Build();

// 4. Initialize Database Schema & Seed Data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbInitializer.SeedAsync(db);
}

// 5. Middleware Pipeline
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CampFix API v1");
    c.RoutePrefix = "swagger";
});

// Enable serving frontend from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// 6. Map Endpoints & SignalR Hub
app.MapHub<IssueHub>("/hubs/issues");
app.MapIssueEndpoints();

Console.WriteLine("==================================================");
Console.WriteLine("  CampFix System Started");
Console.WriteLine("  Web UI:      http://localhost:5000");
Console.WriteLine("  Swagger API: http://localhost:5000/swagger");
Console.WriteLine("==================================================");

app.Run();

// Helper to convert URI-style postgresql:// to Npgsql ADO.NET connection string
static string FormatPostgreSqlConnectionString(string input)
{
    if (input.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        input.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(input);
            var userInfo = uri.UserInfo.Split(':');
            var npgsqlBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "",
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
                Database = uri.AbsolutePath.TrimStart('/'),
                SslMode = SslMode.Require
            };
            return npgsqlBuilder.ConnectionString;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CampFix Warning] Failed to parse DATABASE_URL as URI: {ex.Message}. Using raw string.");
        }
    }
    return input;
}
