using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Rbac.AspNetCore;
using Rbac.AspNetCore.Authorization;
using Rbac.Domain;
using Rbac.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DemoAuthOptions>(builder.Configuration.GetSection("DemoAuth"));
var demoAuth = builder.Configuration.GetSection("DemoAuth").Get<DemoAuthOptions>() ?? new DemoAuthOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(demoAuth.SigningKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = demoAuth.Issuer,
            ValidAudience = demoAuth.Audience,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddRbac(
    options =>
    {
        options.ExternalIdClaimType = JwtRegisteredClaimNames.Sub;
        options.EnableAdminApi = true;
    },
    infrastructure =>
    {
        var sqlServer = builder.Configuration.GetConnectionString("SqlServer");
        if (!string.IsNullOrWhiteSpace(sqlServer))
        {
            infrastructure.SqlServerConnectionString = sqlServer;
        }
        else
        {
            infrastructure.SqliteConnectionString = builder.Configuration.GetConnectionString("Sqlite")
                ?? "Data Source=rbac.sample.db";
        }

        infrastructure.EnsureCreated = true;
        infrastructure.Seed.SeedSystemCatalog = true;
        infrastructure.Seed.SeedDemoData = true;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("sample", policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddSingleton(signingKey);
builder.Services.AddEndpointsApiExplorer();

if (demoAuth.SigningKey.Length < 32)
{
    throw new InvalidOperationException("DemoAuth:SigningKey must be at least 32 characters.");
}

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Cache-Control"] = "no-store";
        return Task.CompletedTask;
    });
    await next();
});
app.UseCors("sample");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseRbac();

app.MapPost("/api/auth/login", (LoginRequest request, IConfiguration config) =>
{
    var users = config.GetSection("DemoAuth:Users").Get<DemoUser[]>() ?? [];
    var user = users.FirstOrDefault(u =>
        string.Equals(u.Username, request.Username, StringComparison.OrdinalIgnoreCase));
    var expected = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(user?.Password ?? "missing-user-dummy-password")));
    var provided = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(request.Password ?? string.Empty)));
    var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expected);
    var providedBytes = System.Text.Encoding.UTF8.GetBytes(provided);
    if (user is null ||
        !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
    {
        return Results.Json(new { error = "Invalid username or password." }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var now = DateTime.UtcNow;
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Username),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
        new Claim(JwtRegisteredClaimNames.Name, user.DisplayName)
    };
    var token = new JwtSecurityToken(
        demoAuth.Issuer,
        demoAuth.Audience,
        claims,
        now,
        now.AddMinutes(demoAuth.ExpiresMinutes),
        new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new
    {
        accessToken = jwt,
        tokenType = "Bearer",
        expiresIn = demoAuth.ExpiresMinutes * 60,
        username = user.Username,
        displayName = user.DisplayName
    });
}).AllowAnonymous().RequireRateLimiting("login");

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

var passengers = app.MapGroup("/api/passengers").RequireAuthorization();
passengers.MapGet("", () => Results.Ok(PassengerStore.All))
    .RequirePermission("passenger.read");
passengers.MapPost("", (Passenger passenger) =>
{
    var created = PassengerStore.Add(passenger);
    return Results.Created($"/api/passengers/{created.Id}", created);
}).RequirePermission("passenger.create");
passengers.MapPut("/{id:guid}", (Guid id, Passenger passenger) =>
{
    var updated = PassengerStore.Update(id, passenger);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
}).RequirePermission("passenger.update");
passengers.MapDelete("/{id:guid}", (Guid id) =>
    PassengerStore.Delete(id) ? Results.NoContent() : Results.NotFound())
    .RequirePermission("passenger.delete");
passengers.MapGet("/export", () => Results.Ok(new
{
    generatedAt = DateTimeOffset.UtcNow,
    rows = PassengerStore.All
})).RequirePermission("passenger.export");

app.MapGet("/api/reports", () => Results.Ok(new
{
    title = "Daily passenger summary",
    total = PassengerStore.All.Count,
    approved = true
})).RequireAuthorization().RequirePermission("report.read");

app.MapGet("/api/reports/export", () => Results.Ok(new
{
    format = "csv",
    content = "id,fullName,documentNo\n" + string.Join('\n', PassengerStore.All.Select(p => $"{p.Id},{p.FullName},{p.DocumentNo}"))
})).RequireAuthorization().RequirePermission("report.export");

if (app.Configuration.GetValue("Rbac:EnableAdminApi", true))
{
    app.MapRbac();
}

app.Run();

public partial class Program;

public sealed class DemoAuthOptions
{
    public string Issuer { get; set; } = "rbac-sample";
    public string Audience { get; set; } = "rbac-sample";
    public string SigningKey { get; set; } = "dev-only-change-me-please-use-32+!";
    public int ExpiresMinutes { get; set; } = 480;
}

public sealed class DemoUser
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed record LoginRequest(string Username, string Password);

public sealed class Passenger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string DocumentNo { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
}

internal static class PassengerStore
{
    private static readonly List<Passenger> Items =
    [
        new() { FullName = "Aung Aung", DocumentNo = "MA123456", Nationality = "MM" },
        new() { FullName = "Jane Cooper", DocumentNo = "US998877", Nationality = "US" },
        new() { FullName = "Hiro Tanaka", DocumentNo = "JP112233", Nationality = "JP" }
    ];

    public static IReadOnlyList<Passenger> All
    {
        get
        {
            lock (Items)
            {
                return Items.ToList();
            }
        }
    }

    public static Passenger Add(Passenger passenger)
    {
        passenger.Id = Guid.NewGuid();
        lock (Items)
        {
            Items.Add(passenger);
        }

        return passenger;
    }

    public static Passenger? Update(Guid id, Passenger passenger)
    {
        lock (Items)
        {
            var existing = Items.FirstOrDefault(p => p.Id == id);
            if (existing is null)
            {
                return null;
            }

            existing.FullName = passenger.FullName;
            existing.DocumentNo = passenger.DocumentNo;
            existing.Nationality = passenger.Nationality;
            return existing;
        }
    }

    public static bool Delete(Guid id)
    {
        lock (Items)
        {
            return Items.RemoveAll(p => p.Id == id) > 0;
        }
    }
}
