using Microsoft.EntityFrameworkCore;
using ChoresApp.Models;
using ChoresApp.Models.DTOs;
using ChoresApp.Helpers;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChoresApp.Endpoints
{
    public static class ChoreLogEndpoints
    {
        private static int GetUserFamilyId(ClaimsPrincipal user)
        {
            var familyIdClaim = user.FindFirst("familyId");
            return familyIdClaim != null && int.TryParse(familyIdClaim.Value, out int familyId) ? familyId : 0;
        }

        public static void MapChoreLogEndpoints(this WebApplication app)
        {
            // Add logentry
            app.MapPost("/api/chorelog/add", async (HttpContext httpContext, ChoresAppDbContext db, ChoreLogDto logDto) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                try
                {
                    var user = await db.ChoreUsers.FindAsync(logDto.UserId);
                    if (user == null || user.FamilyId != userFamilyId)
                    {
                        return Results.Forbid();
                    }

                    var chore = await db.Chores.FindAsync(logDto.ChoreId);
                    if (chore == null)
                    {
                        return Results.NotFound($"Chore with ID {logDto.ChoreId} not found.");
                    }

                    var choreLog = new ChoreLog
                    {
                        IsCompleted = logDto.IsCompleted,
                        DueDate = logDto.DueDate,
                        Chore = chore,
                        ChoreUser = user,
                        UserId = logDto.UserId,
                        ReportedByUserId = logDto.ReportedByUserId
                    };

                    db.ChoresLog.Add(choreLog);
                    await db.SaveChangesAsync();

                    logDto.Id = choreLog.Id;
                    return Results.Created($"/api/chorelog/{choreLog.Id}", logDto);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to add log entry: {ex.Message}");
                }
            }).RequireAuthorization();

            // Update logentry
            app.MapPut("/api/chorelog/{id}", async (HttpContext httpContext, ChoresAppDbContext db, int id, ChoreLogDto logDto) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                try
                {
                    var choreLog = await db.ChoresLog
                        .Include(cl => cl.ChoreUser)
                        .FirstOrDefaultAsync(cl => cl.Id == id);
                    if (choreLog == null || choreLog.ChoreUser.FamilyId != userFamilyId)
                        return Results.Forbid();

                    choreLog.IsCompleted = logDto.IsCompleted;
                    choreLog.DueDate = logDto.DueDate;
                    choreLog.ChoreId = logDto.ChoreId;
                    choreLog.UserId = logDto.UserId;

                    await db.SaveChangesAsync();
                    return Results.Ok(choreLog);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to update log entry: {ex.Message}");
                }
            }).RequireAuthorization();

            // Delete logentry
            app.MapDelete("/api/chorelog/{id}", async (HttpContext httpContext, ChoresAppDbContext db, int id) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                try
                {
                    var choreLog = await db.ChoresLog
                        .Include(cl => cl.ChoreUser)
                        .FirstOrDefaultAsync(cl => cl.Id == id);
                    if (choreLog == null || choreLog.ChoreUser.FamilyId != userFamilyId)
                        return Results.Forbid();

                    db.ChoresLog.Remove(choreLog);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to delete log entry: {ex.Message}");
                }
            }).RequireAuthorization();

            // Get all logentries for a user
            app.MapGet("/api/chorelog/user/{userId}", async (HttpContext httpContext, ChoresAppDbContext db, int userId) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                try
                {
                    var user = await db.ChoreUsers.FindAsync(userId);
                    if (user == null || user.FamilyId != userFamilyId)
                        return Results.Forbid();

                    var logs = await db.ChoresLog
                        .Where(l => l.UserId == userId)
                        .Include(l => l.Chore)
                        .Include(l => l.ChoreUser)
                        .Select(l => new ChoreLogDto
                        {
                            Id = l.Id,
                            IsCompleted = l.IsCompleted,
                            DueDate = l.DueDate,
                            ChoreId = l.ChoreId,
                            UserId = l.UserId,
                            ChoreName = l.Chore.Name,
                            UserName = $"{l.ChoreUser.FirstName} {l.ChoreUser.LastName}",
                            ReportedByUserId = l.ReportedByUserId
                        })
                        .ToListAsync();

                    return Results.Ok(logs);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve log entries: {ex.Message}");
                }
            }).RequireAuthorization();

            // Get all logentries for a family
            app.MapGet("/api/chorelog/family/{familyId}", async (HttpContext httpContext, ChoresAppDbContext db, int familyId) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (familyId != userFamilyId)
                    return Results.Forbid();

                try
                {
                    var logs = await db.ChoresLog
                        .Where(l => l.ChoreUser.FamilyId == familyId)
                        .Include(l => l.Chore)
                        .Include(l => l.ChoreUser)
                        .Select(l => new ChoreLogDto
                        {
                            Id = l.Id,
                            IsCompleted = l.IsCompleted,
                            DueDate = l.DueDate,
                            ChoreId = l.ChoreId,
                            UserId = l.UserId,
                            ChoreName = l.Chore.Name,
                            UserName = $"{l.ChoreUser.FirstName} {l.ChoreUser.LastName}",
                            ReportedByUserId = l.ReportedByUserId
                        })
                        .ToListAsync();

                    return Results.Ok(logs);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve log entries: {ex.Message}");
                }
            }).RequireAuthorization();

            // Get all logentries for a chore
            app.MapGet("/api/chorelog/chore/{choreId}", async (HttpContext httpContext, ChoresAppDbContext db, int choreId) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                try
                {
                    var chore = await db.Chores.FindAsync(choreId);
                    if (chore == null || chore.FamilyId != userFamilyId)
                        return Results.Forbid();

                    var logs = await db.ChoresLog
                        .Where(l => l.ChoreId == choreId)
                        .Include(l => l.Chore)
                        .Include(l => l.ChoreUser)
                        .Select(l => new ChoreLogDto
                        {
                            Id = l.Id,
                            IsCompleted = l.IsCompleted,
                            DueDate = l.DueDate,
                            ChoreId = l.ChoreId,
                            UserId = l.UserId,
                            ChoreName = l.Chore.Name,
                            UserName = $"{l.ChoreUser.FirstName} {l.ChoreUser.LastName}",
                            ReportedByUserId = l.ReportedByUserId
                        })
                        .ToListAsync();

                    return Results.Ok(logs);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve log entries: {ex.Message}");
                }
            }).RequireAuthorization();

            // Delete list of logentries
            app.MapDelete("/api/chorelog/deletemultiple", async (HttpContext httpContext, [FromServices] ChoresAppDbContext db, [FromBody] List<int> ids) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                try
                {
                    var logsToDelete = await db.ChoresLog
                        .Where(l => ids.Contains(l.Id) && l.ChoreUser.FamilyId == userFamilyId)
                        .ToListAsync();

                    db.ChoresLog.RemoveRange(logsToDelete);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to delete log entries: {ex.Message}");
                }
            }).RequireAuthorization();

            // Get all logentries for a family and a specific week
            app.MapGet("/api/chorelog/family/{familyId}/week", async (HttpContext httpContext, ChoresAppDbContext db, int familyId, int year, int weekNumber) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                if (familyId != userFamilyId)
                    return Results.Forbid();

                try
                {
                    var startOfWeek = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Monday);
                    var endOfWeek = startOfWeek.AddDays(7);

                    var logs = await db.ChoresLog
                        .Where(l => l.ChoreUser.FamilyId == familyId && l.DueDate >= startOfWeek && l.DueDate < endOfWeek)
                        .Include(l => l.Chore)
                        .Include(l => l.ChoreUser)
                        .Select(l => new ChoreLogDto
                        {
                            Id = l.Id,
                            IsCompleted = l.IsCompleted,
                            DueDate = l.DueDate,
                            ChoreId = l.ChoreId,
                            UserId = l.UserId,
                            ChoreName = l.Chore.Name,
                            UserName = $"{l.ChoreUser.FirstName} {l.ChoreUser.LastName}",
                            ReportedByUserId = l.ReportedByUserId
                        })
                        .ToListAsync();

                    return Results.Ok(logs);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve log entries: {ex.Message}");
                }
            }).RequireAuthorization();

            // Get the last N entries from ChoreLog
            app.MapGet("/api/chorelog/recent/{count}", async (HttpContext httpContext, ChoresAppDbContext db, int count) =>
            {
                var userFamilyId = GetUserFamilyId(httpContext.User);
                try
                {
                    var recentLogs = await db.ChoresLog
                        .Where(l => l.ChoreUser.FamilyId == userFamilyId)
                        .OrderByDescending(l => l.DueDate)
                        .Take(count)
                        .Include(l => l.Chore)
                        .Include(l => l.ChoreUser)
                        .Select(l => new ChoreLogDto
                        {
                            Id = l.Id,
                            IsCompleted = l.IsCompleted,
                            DueDate = l.DueDate,
                            ChoreId = l.ChoreId,
                            UserId = l.UserId,
                            ChoreName = l.Chore.Name,
                            UserName = $"{l.ChoreUser.FirstName} {l.ChoreUser.LastName}",
                            ReportedByUserId = l.ReportedByUserId
                        })
                        .ToListAsync();

                    return Results.Ok(recentLogs);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve recent log entries: {ex.Message}");
                }
            }).RequireAuthorization();
        }
    }
}
