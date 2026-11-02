using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ChoresApi.Data;
using ChoresApi.Infrastructure;
using ChoresApi.Models;
using ChoresApi.Models.DTOs.Auth;

namespace ChoresApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginDto request, ApplicationDbContext db, IConfiguration config, HttpContext context, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Security");
            var ip = ClientIp.GetClientIp(context);
            var email = request.Email.Trim();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Same work as a wrong password, so timing doesn't reveal which addresses exist
                BCrypt.Net.BCrypt.Verify(request.Password, DummyPasswordHash);
                logger.LogWarning("Failed login: unknown email {Email} from {IP}", email, ip);
                return Results.Unauthorized();
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                logger.LogWarning("Failed login: wrong password for {Email} from {IP}", email, ip);
                return Results.Unauthorized();
            }

            user.LastSeenAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var refreshToken = request.RefreshToken ? await IssueRefreshTokenAsync(db, user) : null;
            return Results.Ok(new TokenResponse(GenerateJwtToken(user, config), refreshToken));
        }).RequireRateLimiting("login");

        // No email verification yet: registering logs the user straight in.
        app.MapPost("/api/auth/register", async (RegisterDto request, ApplicationDbContext db, IConfiguration config) =>
        {
            if (!IsPasswordStrong(request.Password))
                return Results.BadRequest("Password must be at least 8 characters.");

            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("A name is required.");

            var email = request.Email.Trim();
            if (await db.Users.AnyAsync(u => u.Email == email))
                return Results.BadRequest("Email already registered");

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Name = request.Name.Trim(),
                LastSeenAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var refreshToken = request.RefreshToken ? await IssueRefreshTokenAsync(db, user) : null;
            return Results.Ok(new TokenResponse(GenerateJwtToken(user, config), refreshToken));
        }).RequireRateLimiting("register");

        // Swaps a refresh token for a new JWT. The refresh token stays the same and its year starts
        // over, so an app in use is never logged out.
        app.MapPost("/api/auth/refresh", async (RefreshDto request, ApplicationDbContext db, IConfiguration config) =>
        {
            var hash = HashRefreshToken(request.RefreshToken);
            var token = await db.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.ExpiresAt > DateTime.UtcNow);
            if (token == null)
                return Results.Unauthorized();

            token.ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);
            await db.SaveChangesAsync();

            return Results.Ok(new TokenResponse(GenerateJwtToken(token.User, config), request.RefreshToken));
        });

        // Knowing the refresh token is all it takes to end it, so this needs no JWT.
        app.MapPost("/api/auth/logout", async (RefreshDto request, ApplicationDbContext db) =>
        {
            var hash = HashRefreshToken(request.RefreshToken);
            await db.RefreshTokens.Where(t => t.TokenHash == hash).ExecuteDeleteAsync();
            return Results.NoContent();
        });
    }

    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(365);

    private static async Task<string> IssueRefreshTokenAsync(ApplicationDbContext db, User user)
    {
        await db.RefreshTokens.Where(t => t.UserId == user.Id && t.ExpiresAt <= DateTime.UtcNow).ExecuteDeleteAsync();

        var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashRefreshToken(token),
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime)
        });
        await db.SaveChangesAsync();
        return token;
    }

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("password-enumeration-guard");

    internal static bool IsPasswordStrong(string password) => password.Length >= 8;

    private static string GenerateJwtToken(User user, IConfiguration config)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(
            config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
