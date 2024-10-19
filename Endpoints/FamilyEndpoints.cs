using Microsoft.EntityFrameworkCore;
using ChoresApp.Models;
using ChoresApp.Helpers;
using ChoresApi.Models.DTOs;
using System.Security.Claims;

namespace ChoresApp.Endpoints
{
    public static class FamilyEndpoints
    {
        private static int GetUserFamilyId(ClaimsPrincipal user)
        {
            var familyIdClaim = user.FindFirst("familyId");
            return familyIdClaim != null && int.TryParse(familyIdClaim.Value, out int familyId) ? familyId : 0;
        }

        public static void MapFamilyEndpoints(this WebApplication app)
        {
            // Create
            app.MapPost("/api/family/add", async (HttpContext httpContext, ChoresAppDbContext db, FamilyDto familyDto) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (userFamilyId != 0)
                {
                    return Results.BadRequest("User already belongs to a family");
                }

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
            app.MapGet("/api/family/{id}/getallmembers", async (HttpContext httpContext, ChoresAppDbContext db) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (userFamilyId == 0)
                {
                    return Results.BadRequest("User does not belong to a family");
                }

                try
                {
                    return Results.Ok(await db.ChoreUsers.Where(cu => cu.FamilyId == userFamilyId).ToListAsync());
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve families: {ex.Message}");
                }
            }).RequireAuthorization();

            // Read (Get all families) Admin only
            app.MapGet("/api/families/getall", async (HttpContext httpContext, ChoresAppDbContext db) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (userFamilyId == 0)
                {
                    return Results.BadRequest("User does not belong to a family");
                }

                try
                {
                    return Results.Ok(await db.Families.ToListAsync());
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve families: {ex.Message}");
                }
            }).RequireAuthorization("AdminOnly");

            // Read (Get by id)
            app.MapGet("/api/family/{id}", async (HttpContext httpContext, ChoresAppDbContext db, int id) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);

                if (userFamilyId != id)
                {
                    return Results.Forbid();
                }

                try
                {
                    var family = await db.Families.FindAsync(id);
                    return family != null ? Results.Ok(family) : Results.NotFound();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve family: {ex.Message}");
                }
            }).RequireAuthorization();

            // Update
            app.MapPut("/api/family/{id}", async (HttpContext httpContext, ChoresAppDbContext db, int id, Family updatedFamily) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (userFamilyId != id)
                {
                    return Results.Forbid();
                }

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
            }).RequireAuthorization();

            // Delete
            app.MapDelete("/api/family/{id}", async (HttpContext httpContext, ChoresAppDbContext db, int id) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (userFamilyId != id)
                {
                    return Results.Forbid();
                }

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
            }).RequireAuthorization();

            // Get users by family id
            app.MapGet("/api/family/{id}/users", async (HttpContext httpContext, ChoresAppDbContext db, int id) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (userFamilyId != id)
                {
                    return Results.Forbid();
                }

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
            }).RequireAuthorization();
        }
    }
}
