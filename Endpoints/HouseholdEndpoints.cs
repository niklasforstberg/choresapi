using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ChoresApi.Data;
using ChoresApi.Models;
using ChoresApi.Models.DTOs.Household;

namespace ChoresApi.Endpoints;

public static class HouseholdEndpoints
{
    public static void MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/household").RequireAuthorization();

        group.MapGet("/", async (ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var householdId = await CurrentUser.HouseholdIdAsync(db, principal);
            if (householdId == null)
                return Results.NotFound();

            var household = await db.Households
                .Include(h => h.Members)
                .FirstAsync(h => h.Id == householdId);

            return Results.Ok(ToDto(household));
        });

        group.MapPost("/", async (CreateHouseholdDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            if (user == null)
                return Results.Unauthorized();
            if (user.HouseholdId != null)
                return Results.BadRequest("You already belong to a household.");
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("A name is required.");

            var household = new Household { Name = request.Name.Trim(), InviteCode = NewInviteCode() };
            household.Members.Add(user);
            db.Households.Add(household);
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(household));
        });

        group.MapPost("/join", async (JoinHouseholdDto request, ApplicationDbContext db, ClaimsPrincipal principal) =>
        {
            var user = await CurrentUser.FindAsync(db, principal);
            if (user == null)
                return Results.Unauthorized();
            if (user.HouseholdId != null)
                return Results.BadRequest("You already belong to a household.");

            var code = request.InviteCode.Trim().ToUpperInvariant();
            var household = await db.Households
                .Include(h => h.Members)
                .FirstOrDefaultAsync(h => h.InviteCode == code);
            if (household == null)
                return Results.BadRequest("No household has that code.");

            household.Members.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(household));
        }).RequireRateLimiting("join");
    }

    private static HouseholdDto ToDto(Household household) => new(
        household.Id,
        household.Name,
        household.InviteCode,
        household.Members
            .OrderBy(m => m.Name)
            .Select(m => new HouseholdMemberDto(m.Id, m.Name))
            .ToList());

    // No 0/O or 1/I, so the code survives being read aloud or written on a note.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private static string NewInviteCode() => RandomNumberGenerator.GetString(CodeAlphabet, 8);
}
