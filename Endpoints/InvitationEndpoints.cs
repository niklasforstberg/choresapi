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
using ChoresApp.Integrations;

namespace ChoresApp.Endpoints
{
    public static class InvitationEndpoints
    {
        public static void MapInvitationEndpoints(this WebApplication app)
        {
            // Create Invitation
            app.MapPost("/api/invitation/create", async (HttpContext httpContext, ChoresAppDbContext db, InvitationDto invitationDto, SmtpEmailSender emailSender) =>
            {
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                var userFamilyIdClaim = httpContext.User.FindFirst("familyId");

                if (userIdClaim == null || userFamilyIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || !int.TryParse(userFamilyIdClaim.Value, out int userFamilyId))
                {
                    return Results.Unauthorized();
                }

                if (invitationDto.FamilyId != userFamilyId || invitationDto.InviterId != userId)
                {
                    return Results.Forbid();
                }

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

                    // Send invitation email
                    await emailSender.SendInvitationEmail(new InvitationDto
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
                    });

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
            app.MapPost("/api/invitation/{token}/accept", [AllowAnonymous] async (ChoresAppDbContext db, string token) =>
            {
                try
                {
                    Console.WriteLine("Accepting invitation");
                    var invitation = await db.Invitations
                        .Include(i => i.Family)
                        .FirstOrDefaultAsync(i => i.Token == token);

                    if (invitation == null) return Results.NotFound("Invitation not found.");
                    if (invitation.Status != "pending") return Results.BadRequest("Invitation is no longer valid.");
                    if (invitation.ExpiresAt < DateTime.UtcNow) return Results.BadRequest("Invitation has expired.");

                    var user = await db.ChoreUsers.FirstOrDefaultAsync(u => u.Email == invitation.InviteeEmail);
                    bool userExists = user != null;

                    if (user != null)
                    {
                        // User exists, update their family
                        user.FamilyId = invitation.FamilyId;
                    }

                    invitation.Status = "accepted";
                    await db.SaveChangesAsync();

                    // Return response with family information and user existence status
                    var responseData = new
                    {
                        Message = "Invitation accepted successfully.",
                        UserExists = userExists,
                        Family = new
                        {
                            FamilyId = invitation.FamilyId,
                            FamilyName = invitation.Family.Name
                        },
                        InviteeEmail = invitation.InviteeEmail
                    };

                    return Results.Ok(responseData);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to accept invitation: {ex.Message}");
                }
            });

            // Reject Invitation
            app.MapPost("/api/invitation/{token}/reject", [AllowAnonymous] async (ChoresAppDbContext db, string token) =>
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
            app.MapGet("/api/invitations/family/{familyId}", async (HttpContext httpContext, ChoresAppDbContext db, int familyId) =>
            {
                var userFamilyIdClaim = httpContext.User.FindFirst("familyId");
                if (userFamilyIdClaim == null || !int.TryParse(userFamilyIdClaim.Value, out int userFamilyId))
                {
                    return Results.Unauthorized();
                }

                if (familyId != userFamilyId)
                {
                    return Results.Forbid();
                }

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
            app.MapPost("/api/invitation/{id}/resend", async (HttpContext httpContext, ChoresAppDbContext db, int id, SmtpEmailSender emailSender) =>
            {
                var userFamilyIdClaim = httpContext.User.FindFirst("familyId");
                if (userFamilyIdClaim == null || !int.TryParse(userFamilyIdClaim.Value, out int userFamilyId))
                {
                    return Results.Unauthorized();
                }

                try
                {
                    var invitation = await db.Invitations.FindAsync(id);
                    if (invitation == null) return Results.NotFound();

                    if (invitation.FamilyId != userFamilyId)
                    {
                        return Results.Forbid();
                    }

                    invitation.Token = Guid.NewGuid().ToString(); // Generate a new token
                    invitation.CreatedAt = DateTime.UtcNow;
                    invitation.ExpiresAt = DateTime.UtcNow.AddDays(7); // Reset expiration to 7 days from now
                    invitation.Status = "pending";

                    await db.SaveChangesAsync();

                    // Send invitation email
                    await emailSender.SendInvitationEmail(new InvitationDto
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
                    });

                    return Results.Ok("Invitation resent successfully.");
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to resend invitation: {ex.Message}");
                }
            }).RequireAuthorization();

            // Delete Invitation
            app.MapDelete("/api/invitation/delete/{id}", async (HttpContext httpContext, ChoresAppDbContext db, int id) =>
            {
                var userFamilyIdClaim = httpContext.User.FindFirst("familyId");
                if (userFamilyIdClaim == null || !int.TryParse(userFamilyIdClaim.Value, out int userFamilyId))
                {
                    return Results.Unauthorized();
                }

                try
                {
                    var invitation = await db.Invitations.FindAsync(id);
                    if (invitation == null) return Results.NotFound("Invitation not found.");

                    if (invitation.FamilyId != userFamilyId)
                    {
                        return Results.Forbid();
                    }

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
            app.MapPost("/api/invitations/create", async (HttpContext httpContext, ChoresAppDbContext db, List<InvitationDto> invitationDtos, SmtpEmailSender emailSender) =>
            {
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                var userFamilyIdClaim = httpContext.User.FindFirst("familyId");

                if (userIdClaim == null || userFamilyIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || !int.TryParse(userFamilyIdClaim.Value, out int userFamilyId))
                {
                    return Results.Unauthorized();
                }

                try
                {
                    var createdInvitations = new List<InvitationDto>();

                    foreach (var invitationDto in invitationDtos)
                    {
                        if (invitationDto.FamilyId != userFamilyId || invitationDto.InviterId != userId)
                        {
                            return Results.Forbid();
                        }

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

                        invitationDto.Token = invitation.Token;
                        invitationDto.InviterName = $"{inviter.FirstName} {inviter.LastName}";

                        // Send invitation email
                        Console.WriteLine("Sending invitation email");
                        await emailSender.SendInvitationEmail(invitationDto);
                    }

                    return Results.Created($"/api/invitations", invitationDtos);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest($"Failed to create invitations: {ex.Message}");
                }
            }).RequireAuthorization();
        }
    }
}
