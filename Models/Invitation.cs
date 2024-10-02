using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChoresApp.Models
{
    public class Invitation
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int FamilyId { get; set; }
        [Required]
        public int InviterId { get; set; }
        [Required]
        [StringLength(255)]
        public required string InviteeEmail { get; set; }
        [Required]
        [StringLength(20)]
        public string? Status { get; set; }
        [Required]
        [StringLength(255)]
        public string? Token { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        [ForeignKey("FamilyId")]
        public required Family Family { get; set; }
        [ForeignKey("InviterId")]
        public required ChoreUser Inviter { get; set; }
    }
}
