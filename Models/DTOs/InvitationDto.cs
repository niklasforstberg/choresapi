using System;

namespace ChoresApp.Models.DTOs
{
    public class InvitationDto
    {
        public int Id { get; set; }
        public int FamilyId { get; set; }
        public int InviterId { get; set; }
        public string InviteeEmail { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Token { get; set; }

        // We might want to include some additional information about the family and inviter
        public string? FamilyName { get; set; }
        public string? InviterName { get; set; }
    }
}
