using System.Security.Claims;
using ChoresApi.Data;
using ChoresApi.Models;

namespace ChoresApi.Endpoints;

public static class CurrentUser
{
    public static async Task<User?> FindAsync(ApplicationDbContext db, ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return null;
        return await db.Users.FindAsync(userId);
    }

    // The household the caller belongs to, or null when they have none yet.
    public static async Task<int?> HouseholdIdAsync(ApplicationDbContext db, ClaimsPrincipal principal) =>
        (await FindAsync(db, principal))?.HouseholdId;
}
