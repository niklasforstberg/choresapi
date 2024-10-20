using Microsoft.EntityFrameworkCore;
using ChoresApp.Models;
using ChoresApi.Models.DTOs;
using ChoresApp.Helpers;
using System.Security.Claims;

namespace ChoresApp.Endpoints
{
    public static class ChoreEndpoints
    {
        private static int GetUserFamilyId(ClaimsPrincipal user)
        {
            var familyIdClaim = user.FindFirst("familyId");
            return familyIdClaim != null && int.TryParse(familyIdClaim.Value, out int familyId) ? familyId : 0;
        }

        public static void MapChoreEndpoints(this WebApplication app)
        {
            // Add
            app.MapPost("/api/chore/add", async (HttpContext httpContext, ChoresAppDbContext db, ChoreDto choreDto) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (userFamilyId != choreDto.FamilyId)
                {
                    return Results.Forbid();
                }

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
            app.MapPut("/api/chore/{id}", async (HttpContext httpContext, ChoresAppDbContext db, int id, ChoreDto updatedChore) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (userFamilyId != updatedChore.FamilyId)
                {
                    return Results.Forbid();
                }

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
            app.MapDelete("/api/chore/delete/{id}", async (HttpContext httpContext, ChoresAppDbContext db, int id) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);

                try
                {
                    var chore = await db.Chores.FindAsync(id);
                    if (chore == null) return Results.NotFound();

                    if (userFamilyId != chore.FamilyId)
                    {
                        return Results.Forbid();
                    }

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
            app.MapGet("/api/chore/getall/{familyId}", async (HttpContext httpContext, ChoresAppDbContext db, int familyId) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (userFamilyId != familyId)
                {
                    return Results.Forbid();
                }

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
            app.MapPost("/api/chore/deletemany", async (HttpContext httpContext, ChoresAppDbContext db, List<int> choreIds) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);

                try
                {
                    var choresToDelete = await db.Chores
                        .Where(c => choreIds.Contains(c.Id) && c.FamilyId == userFamilyId)
                        .ToListAsync();

                    if (choresToDelete.Count == 0)
                        return Results.NotFound("No chores found with the provided ids for your family");

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
