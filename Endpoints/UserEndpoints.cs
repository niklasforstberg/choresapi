using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChoresApi.Data;
using ChoresApi.Models;
using ChoresApi.Models.DTOs.User;

namespace ChoresApi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/me").RequireAuthorization();

        group.MapGet("/", async (ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            if (user == null)
                return Results.Unauthorized();

            return Results.Ok(ToDto(user));
        });

        group.MapPut("/", async (UpdateProfileDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            if (user == null)
                return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("A name is required.");

            var email = request.Email.Trim();
            if (email != user.Email)
            {
                // The email is the login, so changing it takes the password like changing the password does.
                if (request.CurrentPassword == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                    return Results.BadRequest("Incorrect password.");
                if (!email.Contains('@'))
                    return Results.BadRequest("A valid email is required.");
                if (await db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
                    return Results.BadRequest("Email already registered");
                user.Email = email;
            }

            user.Name = request.Name.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(user));
        });

        // Erases the account: its claims, completions and refresh tokens go with it, completions it recorded
        // for others lose their recorder, and a household left without members is deleted with its chores.
        group.MapDelete("/", async ([FromBody] DeleteAccountDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            if (user == null)
                return Results.Unauthorized();
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Results.BadRequest("Incorrect password.");

            await using var transaction = await db.Database.BeginTransactionAsync();
            var householdId = user.HouseholdId;
            db.Users.Remove(user);
            await db.SaveChangesAsync();

            if (householdId != null && !await db.Users.AnyAsync(u => u.HouseholdId == householdId))
                await db.Households.Where(h => h.Id == householdId).ExecuteDeleteAsync();

            await transaction.CommitAsync();
            return Results.NoContent();
        }).RequireRateLimiting("login");

        group.MapPut("/color", async (UpdateColorDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            if (user == null)
                return Results.Unauthorized();
            if (request.Color != null && !UserColors.All.Contains(request.Color))
                return Results.BadRequest("Unknown colour.");
            if (request.Color != null && user.HouseholdId != null &&
                await db.Users.AnyAsync(u => u.HouseholdId == user.HouseholdId && u.Id != user.Id && u.Color == request.Color))
                return Results.BadRequest("Someone in the household already has that colour.");

            user.Color = request.Color;
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(user));
        });

        group.MapPut("/password", async (ChangePasswordDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            if (user == null)
                return Results.Unauthorized();
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return Results.BadRequest("Incorrect password.");
            if (!AuthEndpoints.IsPasswordStrong(request.NewPassword))
                return Results.BadRequest("Password must be at least 8 characters.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await db.SaveChangesAsync();

            // A new password logs out every other device, in case the old one leaked. Their JWTs still
            // run out within a day.
            var keep = request.RefreshToken == null ? null : AuthEndpoints.HashRefreshToken(request.RefreshToken);
            await db.RefreshTokens.Where(t => t.UserId == user.Id && t.TokenHash != keep).ExecuteDeleteAsync();

            return Results.NoContent();
        }).RequireRateLimiting("login");
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.Name, user.Role.ToString(), user.HouseholdId, user.Color);
}
