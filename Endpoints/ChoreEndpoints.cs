using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ChoresApi.Data;
using ChoresApi.Models;
using ChoresApi.Models.DTOs.Chore;

namespace ChoresApi.Endpoints;

public static class ChoreEndpoints
{
    public static void MapChoreEndpoints(this IEndpointRouteBuilder app)
    {
        // Any member may edit the household's chore list.
        var chores = app.MapGroup("/api/chores").RequireAuthorization();

        chores.MapGet("/", async (ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var householdId = await CurrentUser.HouseholdIdAsync(db, principal);
            if (householdId == null)
                return Results.NotFound();

            var list = await db.Chores
                .Where(c => c.HouseholdId == householdId)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
                .Select(c => new ChoreDto(c.Id, c.Name, c.WeeklyTarget, c.SortOrder))
                .ToListAsync();

            return Results.Ok(list);
        });

        chores.MapPost("/", async (SaveChoreDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var householdId = await CurrentUser.HouseholdIdAsync(db, principal);
            if (householdId == null)
                return Results.NotFound();
            if (Validate(request) is { } error)
                return Results.BadRequest(error);

            var chore = new Chore
            {
                HouseholdId = householdId.Value,
                Name = request.Name.Trim(),
                WeeklyTarget = request.WeeklyTarget,
                SortOrder = request.SortOrder
            };
            db.Chores.Add(chore);
            await db.SaveChangesAsync();

            return Results.Ok(new ChoreDto(chore.Id, chore.Name, chore.WeeklyTarget, chore.SortOrder));
        });

        chores.MapPut("/{id:int}", async (int id, SaveChoreDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var householdId = await CurrentUser.HouseholdIdAsync(db, principal);
            var chore = await db.Chores.FirstOrDefaultAsync(c => c.Id == id && c.HouseholdId == householdId);
            if (chore == null)
                return Results.NotFound();
            if (Validate(request) is { } error)
                return Results.BadRequest(error);

            chore.Name = request.Name.Trim();
            chore.WeeklyTarget = request.WeeklyTarget;
            chore.SortOrder = request.SortOrder;
            await db.SaveChangesAsync();

            return Results.Ok(new ChoreDto(chore.Id, chore.Name, chore.WeeklyTarget, chore.SortOrder));
        });

        // Deletes the chore's entries too, including past weeks.
        chores.MapDelete("/{id:int}", async (int id, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var householdId = await CurrentUser.HouseholdIdAsync(db, principal);
            var chore = await db.Chores.FirstOrDefaultAsync(c => c.Id == id && c.HouseholdId == householdId);
            if (chore == null)
                return Results.NotFound();

            db.Chores.Remove(chore);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Everyone sees the whole household's week, but plans and ticks off only their own entries.
        var entries = app.MapGroup("/api/entries").RequireAuthorization();

        entries.MapGet("/", async (DateOnly weekStart, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var householdId = await CurrentUser.HouseholdIdAsync(db, principal);
            if (householdId == null)
                return Results.NotFound();

            var weekEnd = weekStart.AddDays(7);
            var list = await db.ChoreEntries
                .Where(e => e.Chore.HouseholdId == householdId && e.Date >= weekStart && e.Date < weekEnd)
                .OrderBy(e => e.CreatedAt)
                .Select(e => new ChoreEntryDto(e.Id, e.ChoreId, e.UserId, e.Date, e.CompletedAt != null))
                .ToListAsync();

            return Results.Ok(list);
        });

        entries.MapPost("/", async (CreateChoreEntryDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            if (user?.HouseholdId == null)
                return Results.NotFound();
            if (!await db.Chores.AnyAsync(c => c.Id == request.ChoreId && c.HouseholdId == user.HouseholdId))
                return Results.NotFound();

            var entry = new ChoreEntry { ChoreId = request.ChoreId, UserId = user.Id, Date = request.Date };
            db.ChoreEntries.Add(entry);
            await db.SaveChangesAsync();

            return Results.Ok(new ChoreEntryDto(entry.Id, entry.ChoreId, entry.UserId, entry.Date, false));
        });

        entries.MapPatch("/{id:int}", async (int id, UpdateChoreEntryDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            var entry = await db.ChoreEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == user!.Id);
            if (entry == null)
                return Results.NotFound();

            entry.CompletedAt = request.Completed ? entry.CompletedAt ?? DateTime.UtcNow : null;
            await db.SaveChangesAsync();

            return Results.Ok(new ChoreEntryDto(entry.Id, entry.ChoreId, entry.UserId, entry.Date, entry.CompletedAt != null));
        });

        entries.MapDelete("/{id:int}", async (int id, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            var entry = await db.ChoreEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == user!.Id);
            if (entry == null)
                return Results.NotFound();

            db.ChoreEntries.Remove(entry);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static string? Validate(SaveChoreDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "A name is required.";
        if (request.WeeklyTarget is < 1)
            return "The weekly target must be at least 1, or empty.";
        return null;
    }
}
