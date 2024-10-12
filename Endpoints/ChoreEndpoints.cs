using Microsoft.EntityFrameworkCore;
using ChoresApp.Models;
using ChoresApi.Models.DTOs;
using ChoresApp.Helpers;

namespace ChoresApp.Endpoints
{
    public static class ChoreEndpoints
    {
        public static void MapChoreEndpoints(this WebApplication app)
        {
            // Add
            app.MapPost("/api/chore/add", async (ChoresAppDbContext db, ChoreDto choreDto) =>
            {
                try
                {
                    var chore = new Chore
                    {
                        Name = choreDto.Name,
                        Description = choreDto.Description,
                        FamilyId = choreDto.FamilyId
                    };

                    db.Chores.Add(chore);
                    await db.SaveChangesAsync();

                    choreDto.Id = chore.Id;
                    return Results.Created($"/api/chore/{chore.Id}", choreDto);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to add chore: {ex.Message}");
                }
            }).RequireAuthorization();

            // Update
            app.MapPut("/api/chore/{id}", async (ChoresAppDbContext db, int id, ChoreDto updatedChore) =>
            {
                try
                {
                    var chore = await db.Chores.FindAsync(id);
                    if (chore == null) return Results.NotFound();

                    chore.Name = updatedChore.Name;
                    chore.Description = updatedChore.Description;
                    chore.FamilyId = updatedChore.FamilyId;

                    await db.SaveChangesAsync();
                    return Results.Ok(chore);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to update chore: {ex.Message}");
                }
            }).RequireAuthorization();

            // Delete
            app.MapDelete("/api/chore/delete/{id}", async (ChoresAppDbContext db, int id) =>
            {
                try
                {
                    var chore = await db.Chores.FindAsync(id);
                    if (chore == null) return Results.NotFound();

                    db.Chores.Remove(chore);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to delete chore: {ex.Message}");
                }
            }).RequireAuthorization();

            // Get all (for one family)
            app.MapGet("/api/chore/getall/{familyId}", async (ChoresAppDbContext db, int familyId) =>
            {
                try
                {
                    var chores = await db.Chores
                        .Where(c => c.FamilyId == familyId)
                        .ToListAsync();

                    if (chores.Count == 0)
                        return Results.Ok(new List<Chore>());

                    return Results.Ok(chores);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve chores for family: {ex.Message}");
                }
            }).RequireAuthorization();

            // Delete list of Chores
            app.MapPost("/api/chore/deletemany", async (ChoresAppDbContext db, List<int> choreIds) =>
            {
                try
                {
                    var choresToDelete = await db.Chores
                        .Where(c => choreIds.Contains(c.Id))
                        .ToListAsync();

                    if (choresToDelete.Count == 0)
                        return Results.NotFound("No chores found with the provided ids");

                    db.Chores.RemoveRange(choresToDelete);
                    await db.SaveChangesAsync();
                    return Results.Ok($"Successfully deleted {choresToDelete.Count} chores");
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to delete chores: {ex.Message}");
                }
            }).RequireAuthorization();
        }
    }
}
