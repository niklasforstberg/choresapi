using System.Security.Claims;
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

            return Results.NoContent();
        }).RequireRateLimiting("login");
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.Name, user.Role.ToString(), user.HouseholdId);
}
