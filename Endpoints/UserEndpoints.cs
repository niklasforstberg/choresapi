using System.Security.Claims;
using ChoresApi.Data;
using ChoresApi.Models.DTOs.User;

namespace ChoresApi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/me", async (ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var user = await db.Users.FindAsync(userId);
            if (user == null)
                return Results.Unauthorized();

            return Results.Ok(new UserDto(user.Id, user.Email, user.Name, user.Role.ToString()));
        })
        .RequireAuthorization();
    }
}
