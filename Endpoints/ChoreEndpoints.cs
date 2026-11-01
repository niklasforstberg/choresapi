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

        // Everyone sees the whole household's claims and completions. A claim takes a chore on today or a day
        // to come and is only the member's own to make or drop. A completion records one done today or earlier;
        // any member may record it for another, and either of them may remove it.
        var claims = app.MapGroup("/api/claims").RequireAuthorization();

        claims.MapGet("/", async (DateOnly from, DateOnly to, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var householdId = await CurrentUser.HouseholdIdAsync(db, principal);
            if (householdId == null)
                return Results.NotFound();

            var list = await db.ChoreClaims
                .Where(c => c.Chore.HouseholdId == householdId && c.Date >= from && c.Date <= to)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ChoreDayEntryDto(c.Id, c.ChoreId, c.UserId, c.Date))
                .ToListAsync();

            return Results.Ok(list);
        });

        claims.MapPost("/", async (CreateChoreDayEntryDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            if (user?.HouseholdId == null)
                return Results.NotFound();
            if (!await db.Chores.AnyAsync(c => c.Id == request.ChoreId && c.HouseholdId == user.HouseholdId))
                return Results.NotFound();
            if (request.Date < Today())
                return Results.BadRequest("Days that have passed can't be planned.");

            var claim = new ChoreClaim { ChoreId = request.ChoreId, UserId = user.Id, Date = request.Date };
            db.ChoreClaims.Add(claim);
            await db.SaveChangesAsync();

            return Results.Ok(new ChoreDayEntryDto(claim.Id, claim.ChoreId, claim.UserId, claim.Date));
        });

        // Doing a claimed chore turns the claim into a completion on the claimed day. Any member may tick off
        // another's claim, e.g. one they forgot.
        claims.MapPost("/{id:int}/complete", async (int id, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            var claim = await db.ChoreClaims.FirstOrDefaultAsync(c => c.Id == id && c.Chore.HouseholdId == user!.HouseholdId);
            if (claim == null)
                return Results.NotFound();
            if (claim.Date > Today())
                return Results.BadRequest("A chore can't be done ahead of its day.");

            var completion = new ChoreCompletion { ChoreId = claim.ChoreId, UserId = claim.UserId, Date = claim.Date, RecordedById = user!.Id };
            db.ChoreClaims.Remove(claim);
            db.ChoreCompletions.Add(completion);
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(completion));
        });

        claims.MapDelete("/{id:int}", async (int id, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            var claim = await db.ChoreClaims.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user!.Id);
            if (claim == null)
                return Results.NotFound();

            db.ChoreClaims.Remove(claim);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        var completions = app.MapGroup("/api/completions").RequireAuthorization();

        completions.MapGet("/", async (DateOnly from, DateOnly to, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var householdId = await CurrentUser.HouseholdIdAsync(db, principal);
            if (householdId == null)
                return Results.NotFound();

            var list = await db.ChoreCompletions
                .Where(c => c.Chore.HouseholdId == householdId && c.Date >= from && c.Date <= to)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ChoreCompletionDto(c.Id, c.ChoreId, c.UserId, c.Date, c.RecordedById))
                .ToListAsync();

            return Results.Ok(list);
        });

        // Logs a chore as done without claiming it first, e.g. one forgotten on a day that has passed.
        completions.MapPost("/", async (CreateChoreCompletionDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            if (user?.HouseholdId == null)
                return Results.NotFound();
            if (!await db.Chores.AnyAsync(c => c.Id == request.ChoreId && c.HouseholdId == user.HouseholdId))
                return Results.NotFound();
            var doerId = request.UserId ?? user.Id;
            if (!await db.Users.AnyAsync(u => u.Id == doerId && u.HouseholdId == user.HouseholdId))
                return Results.NotFound();
            if (request.Date > Today())
                return Results.BadRequest("A chore can't be done ahead of its day.");

            var completion = new ChoreCompletion { ChoreId = request.ChoreId, UserId = doerId, Date = request.Date, RecordedById = user.Id };
            db.ChoreCompletions.Add(completion);
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(completion));
        });

        completions.MapDelete("/{id:int}", async (int id, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            var completion = await db.ChoreCompletions.FirstOrDefaultAsync(c => c.Id == id && (c.UserId == user!.Id || c.RecordedById == user.Id));
            if (completion == null)
                return Results.NotFound();

            db.ChoreCompletions.Remove(completion);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static ChoreCompletionDto ToDto(ChoreCompletion c) => new(c.Id, c.ChoreId, c.UserId, c.Date, c.RecordedById);

    // The household lives in Sweden, while the server clock runs on UTC.
    private static DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Stockholm"));

    private static string? Validate(SaveChoreDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "A name is required.";
        if (request.WeeklyTarget is < 1)
            return "The weekly target must be at least 1, or empty.";
        return null;
    }
}
