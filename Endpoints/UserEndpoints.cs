using Microsoft.EntityFrameworkCore;
using ChoresApp.Models;
using ChoresApp.Helpers;

namespace ChoresApp.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            // Create
            app.MapPost("/api/user/add", async (ChoresAppDbContext db, User user) =>
            {
                try
                {
                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                    return Results.Created($"/users/{user.Id}", user);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to add user: {ex.Message}");
                }
            });

            // Read (Get all)
            app.MapGet("/api/user/getall", async (ChoresAppDbContext db) =>
            {
                try
                {
                    return Results.Ok(await db.Users.ToListAsync());
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve users: {ex.Message}");
                }
            });

            // Read (Get by id)
            app.MapGet("/api/user/{id}", async (ChoresAppDbContext db, int id) =>
            {
                try
                {
                    var user = await db.Users.FindAsync(id);
                    return user != null ? Results.Ok(user) : Results.NotFound();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve user: {ex.Message}");
                }
            });

            // Update
            app.MapPut("/api/user/{id}", async (ChoresAppDbContext db, int id, User updatedUser) =>
            {
                try
                {
                    var user = await db.Users.FindAsync(id);
                    if (user == null) return Results.NotFound();

                    user.Username = updatedUser.Username;
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

                    await db.SaveChangesAsync();
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to update user: {ex.Message}");
                }
            });

            // Delete
            app.MapDelete("/users/{id}", async (ChoresAppDbContext db, int id) =>
            {
                try
                {
                    var user = await db.Users.FindAsync(id);
                    if (user == null) return Results.NotFound();

                    db.Users.Remove(user);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to delete user: {ex.Message}");
                }
            });
        }
    }
}
