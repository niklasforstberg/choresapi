using Microsoft.EntityFrameworkCore;
using ChoresApp.Models;
using ChoresApp.Helpers;
using ChoresApi.Models.DTOs;

namespace ChoresApp.Endpoints
{
    public static class FamilyEndpoints
    {
        public static void MapFamilyEndpoints(this WebApplication app)
        {
            // Create
            app.MapPost("/api/family/add", async (ChoresAppDbContext db, FamilyDto familyDto) =>
            {
                try
                {
                    var family = new Family
                    {
                        Name = familyDto.Name,
                        CreatedBy = familyDto.CreatedBy,
                        CreatedAt = DateTime.Now
                    };
                
                    db.Families.Add(family);
                    await db.SaveChangesAsync();

                    // Find the user by Id and update their FamilyId
                    var choreUser = await db.ChoreUsers.FindAsync(familyDto.CreatedBy);
                    if (choreUser != null)
                    {
                        choreUser.FamilyId = family.Id;
                        await db.SaveChangesAsync();
                    }

                    familyDto.Id = family.Id;
                    familyDto.CreatedAt = family.CreatedAt;
                    Console.WriteLine("Family created: " + familyDto);
                    return Results.Created($"/api/family/{family.Id}", familyDto);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to add family: {ex.Message}");
                }
            }).RequireAuthorization();

            // Read (Get all)
            app.MapGet("/api/family/getall", async (ChoresAppDbContext db) =>
            {
                try
                {
                    return Results.Ok(await db.Families.ToListAsync());
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve families: {ex.Message}");
                }
            });

            // Read (Get by id)
            app.MapGet("/api/family/{id}", async (ChoresAppDbContext db, int id) =>
            {
                try
                {
                    var family = await db.Families.FindAsync(id);
                    return family != null ? Results.Ok(family) : Results.NotFound();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve family: {ex.Message}");
                }
            });

            // Update
            app.MapPut("/api/family/{id}", async (ChoresAppDbContext db, int id, Family updatedFamily) =>
            {
                try
                {
                    var family = await db.Families.FindAsync(id);
                    if (family == null) return Results.NotFound();

                    family.Name = updatedFamily.Name;
                    // Update other properties as needed

                    await db.SaveChangesAsync();
                    return Results.Ok(family);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to update family: {ex.Message}");
                }
            });

            // Delete
            app.MapDelete("/api/family/{id}", async (ChoresAppDbContext db, int id) =>
            {
                try
                {
                    var family = await db.Families.FindAsync(id);
                    if (family == null) return Results.NotFound();

                    db.Families.Remove(family);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to delete family: {ex.Message}");
                }
            });

            // Get users by family id
            app.MapGet("/api/family/{id}/users", async (ChoresAppDbContext db, int id) =>
            {
                try
                {
                    var users = await db.ChoreUsers
                        .Where(u => u.FamilyId == id)
                        .ToListAsync();

                    if (users.Count == 0)
                        return Results.NotFound($"No users found for family with id {id}");

                    return Results.Ok(users);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve users for family: {ex.Message}");
                }
            });
        }
    }
}
