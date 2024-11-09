using Microsoft.EntityFrameworkCore;
using ChoresApp.Models;
using ChoresApp.Helpers;
using System.Security.Claims;

namespace ChoresApp.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            // Create
            app.MapPost("/api/user/add", async (ChoresAppDbContext db, ChoreUser choreUser) =>
            {
                try
                {
                    db.ChoreUsers.Add(choreUser);
                    await db.SaveChangesAsync();
                    return Results.Created($"/users/{choreUser.Id}", choreUser);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to add user: {ex.Message}");
                }
            }).RequireAuthorization();

            // Read (Get all) - Admin only
            app.MapGet("/api/user/getall", async (ChoresAppDbContext db) =>
            {
                try
                {
                    return Results.Ok(await db.ChoreUsers.ToListAsync());
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve users: {ex.Message}");
                }
            }).RequireAuthorization("Admin");

            // Read (Get by id)
            app.MapGet("/api/user/{id}", async (ChoresAppDbContext db, int id) =>
            {
                try
                {
                    var user = await db.ChoreUsers.FindAsync(id);
                    return user != null ? Results.Ok(user) : Results.NotFound();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve user: {ex.Message}");
                }
            }).RequireAuthorization();

            // Update
            app.MapPut("/api/user/{id}", async (ChoresAppDbContext db, int id, ChoreUser updatedUser) =>
            {
                try
                {
                    var user = await db.ChoreUsers.FindAsync(id);
                    if (user == null) return Results.NotFound();

                    user.Password = updatedUser.Password;
                    user.Email = updatedUser.Email;
                    user.Role = updatedUser.Role;
                    user.FirstName = updatedUser.FirstName;
                    user.LastName = updatedUser.LastName;
                    user.PhoneNumber = updatedUser.PhoneNumber;
                    user.Address = updatedUser.Address;
                    user.City = updatedUser.City;
                    user.State = updatedUser.State;
                    user.ZipCode = updatedUser.ZipCode;
                    user.Country = updatedUser.Country;
                    user.FamilyId = updatedUser.FamilyId;

                    await db.SaveChangesAsync();
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to update user: {ex.Message}");
                }
            }).RequireAuthorization();

            // Delete
            app.MapDelete("api/users/{id}", async (ChoresAppDbContext db, int id) =>
            {
                try
                {
                    var user = await db.ChoreUsers.FindAsync(id);
                    if (user == null) return Results.NotFound();

                    db.ChoreUsers.Remove(user);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to delete user: {ex.Message}");
                }
            }).RequireAuthorization();

            // Get detailed user info (me endpoint)
            app.MapGet("/api/user/me", async (ChoresAppDbContext db, ClaimsPrincipal user) =>
            {
                try
                {
                    var userId = user.FindFirst("id")?.Value;
                    if (userId == null) return Results.Unauthorized();

                    var currentUser = await db.ChoreUsers
                        .Include(u => u.Family)
                        .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

                    if (currentUser == null) return Results.NotFound();

                    return Results.Ok(new
                    {
                        id = currentUser.Id,
                        email = currentUser.Email,
                        firstName = currentUser.FirstName,
                        lastName = currentUser.LastName,
                        role = currentUser.Role,
                        familyId = currentUser.FamilyId,
                        familyName = currentUser.Family?.Name,
                        phoneNumber = currentUser.PhoneNumber,
                        address = currentUser.Address,
                        city = currentUser.City,
                        state = currentUser.State,
                        zipCode = currentUser.ZipCode,
                        country = currentUser.Country
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve user info: {ex.Message}");
                }
            }).RequireAuthorization();
        }
    }
}
