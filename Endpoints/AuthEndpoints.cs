using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

            return Results.Ok(new TokenResponse(GenerateJwtToken(user, config)));
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

            return Results.Ok(new TokenResponse(GenerateJwtToken(user, config)));
        }).RequireRateLimiting("register");
    }

    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("password-enumeration-guard");

    private static bool IsPasswordStrong(string password) => password.Length >= 8;

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
