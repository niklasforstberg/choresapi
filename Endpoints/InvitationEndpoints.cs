using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Azure.Core;
using ChoresApp.Models;
using ChoresApp.Models.DTOs;
using ChoresApp.Helpers;

namespace ChoresApp.Endpoints
{
    public static class InvitationEndpoints
    {
        public static void MapInvitationEndpoints(this WebApplication app)
        {
            // Create Invitation
            app.MapPost("/api/invitation/create", async (ChoresAppDbContext db, InvitationDto invitationDto) =>
            {
                try
                {
                    var invitation = new Invitation
                    {
                        FamilyId = invitationDto.FamilyId,
                        Inviter = new ChoreUser { Id = invitationDto.InviterId }, // Create a ChoreUser object
                        Family = new Family { Id = invitationDto.FamilyId },
                        InviterId = invitationDto.InviterId!,
                        InviteeEmail = invitationDto.InviteeEmail!,
                        Status = "pending",
                        Token = Guid.NewGuid().ToString(), // Generate a unique token
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(7) // Set expiration to 7 days from now
                    };

                    db.Invitations.Add(invitation);
                    await db.SaveChangesAsync();

                    // TODO: Send invitation email

                    return Results.Created($"/api/invitation/{invitation.Id}", new InvitationDto
                    {
                        Id = invitation.Id,
                        FamilyId = invitation.FamilyId,
                        InviterId = invitation.InviterId,
                        InviteeEmail = invitation.InviteeEmail,
                        Status = invitation.Status,
                        CreatedAt = invitation.CreatedAt,
                        ExpiresAt = invitation.ExpiresAt
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to create invitation: {ex.Message}");
                }
            }).RequireAuthorization();

            // Get Invitation by Token
            app.MapGet("/api/invitation/{token}", async (ChoresAppDbContext db, string token) =>
            {
                try
                {
                    var invitation = await db.Invitations
                        .Include(i => i.Family)
                        .Include(i => i.Inviter)
                        .FirstOrDefaultAsync(i => i.Token == token);

                    if (invitation == null) return Results.NotFound();

                    var invitationDto = new InvitationDto
                    {
                        Id = invitation.Id,
                        FamilyId = invitation.FamilyId,
                        InviterId = invitation.InviterId,
                        InviteeEmail = invitation.InviteeEmail,
                        Status = invitation.Status,
                        CreatedAt = invitation.CreatedAt,
                        ExpiresAt = invitation.ExpiresAt,
                        FamilyName = invitation.Family.Name,
                        InviterName = $"{invitation.Inviter.FirstName} {invitation.Inviter.LastName}"
                    };

                    return Results.Ok(invitationDto);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve invitation: {ex.Message}");
                }
            });

            // Accept Invitation
            app.MapPost("/api/invitation/{token}/accept", async (ChoresAppDbContext db, string token, int userId) =>
            {
                try
                {
                    var invitation = await db.Invitations.FindAsync(token);
                    if (invitation == null) return Results.NotFound();

                    if (invitation.Status != "pending") return Results.BadRequest("Invitation is no longer valid.");
                    if (invitation.ExpiresAt < DateTime.UtcNow) return Results.BadRequest("Invitation has expired.");

                    var user = await db.ChoreUsers.FindAsync(userId);
                    if (user == null) return Results.NotFound("User not found.");

                    user.FamilyId = invitation.FamilyId;
                    invitation.Status = "accepted";

                    await db.SaveChangesAsync();
                    return Results.Ok("Invitation accepted successfully.");
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to accept invitation: {ex.Message}");
                }
            }).RequireAuthorization();

            // Reject Invitation
            app.MapPost("/api/invitation/{token}/reject", async (ChoresAppDbContext db, string token) =>
            {
                try
                {
                    var invitation = await db.Invitations.FindAsync(token);
                    if (invitation == null) return Results.NotFound();

                    invitation.Status = "rejected";
                    await db.SaveChangesAsync();
                    return Results.Ok("Invitation rejected successfully.");
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to reject invitation: {ex.Message}");
                }
            }).RequireAuthorization();

            // Get Pending Invitations for a Family
            app.MapGet("/api/family/{familyId}/invitations", async (ChoresAppDbContext db, int familyId) =>
            {
                try
                {
                    var invitations = await db.Invitations
                        .Where(i => i.FamilyId == familyId && i.Status == "pending")
                        .Select(i => new InvitationDto
                        {
                            Id = i.Id,
                            FamilyId = i.FamilyId,
                            InviterId = i.InviterId,
                            InviteeEmail = i.InviteeEmail,
                            Status = i.Status,
                            CreatedAt = i.CreatedAt,
                            ExpiresAt = i.ExpiresAt,
                            FamilyName = i.Family.Name,
                            InviterName = $"{i.Inviter.FirstName} {i.Inviter.LastName}"
                        })
                        .ToListAsync();

                    return Results.Ok(invitations);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to retrieve invitations: {ex.Message}");
                }
            }).RequireAuthorization();

            // Resend Invitation
            app.MapPost("/api/invitation/{id}/resend", async (ChoresAppDbContext db, int id) =>
            {
                try
                {
                    var invitation = await db.Invitations.FindAsync(id);
                    if (invitation == null) return Results.NotFound();

                    invitation.Token = Guid.NewGuid().ToString(); // Generate a new token
                    invitation.CreatedAt = DateTime.UtcNow;
                    invitation.ExpiresAt = DateTime.UtcNow.AddDays(7); // Reset expiration to 7 days from now
                    invitation.Status = "pending";

                    await db.SaveChangesAsync();

                    // TODO: Send invitation email

                    return Results.Ok("Invitation resent successfully.");
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to resend invitation: {ex.Message}");
                }
            }).RequireAuthorization();

            // Delete Invitation
            app.MapDelete("/api/invitation/delete/{id}", async (ChoresAppDbContext db, int id) =>
            {
                try
                {
                    var invitation = await db.Invitations.FindAsync(id);
                    if (invitation == null) return Results.NotFound("Invitation not found.");

                    db.Invitations.Remove(invitation);
                    await db.SaveChangesAsync();

                    return Results.Ok("Invitation deleted successfully.");
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to delete invitation: {ex.Message}");
                }
            }).RequireAuthorization();

            // Create Invitations
            app.MapPost("/api/invitations/create", async (ChoresAppDbContext db, List<InvitationDto> invitationDtos) =>
            {
                try
                {
                    var createdInvitations = new List<InvitationDto>();

                    foreach (var invitationDto in invitationDtos)
                    {
                        // Fetch the Family and ChoreUser (Inviter) from the database
                        var family = await db.Families.FindAsync(invitationDto.FamilyId);
                        var inviter = await db.ChoreUsers.FindAsync(invitationDto.InviterId);

                        if (family == null || inviter == null)
                        {
                         return Results.BadRequest($"Failed to find family or inviter in the database.");
                        }

                        var invitation = new Invitation
                        {
                            FamilyId = invitationDto.FamilyId,
                            Family = family,
                            InviterId = invitationDto.InviterId!,
                            Inviter = inviter,
                            InviteeEmail = invitationDto.InviteeEmail!,
                            Status = "pending",
                            Token = Guid.NewGuid().ToString(),
                            CreatedAt = DateTime.UtcNow,
                            ExpiresAt = DateTime.UtcNow.AddDays(7)
                        };

                        db.Invitations.Add(invitation);
                        await db.SaveChangesAsync();

                        // TODO: Send invitation email

                        createdInvitations.Add(new InvitationDto
                        {
                            Id = invitation.Id,
                            FamilyId = invitation.FamilyId,
                            InviterId = invitation.InviterId,
                            InviteeEmail = invitation.InviteeEmail,
                            Status = invitation.Status,
                            CreatedAt = invitation.CreatedAt,
                            ExpiresAt = invitation.ExpiresAt
                        });
                    }

                    return Results.Created($"/api/invitations", createdInvitations);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to create invitations: {ex.Message}");
                }
            }).RequireAuthorization();
        }
    }
}
