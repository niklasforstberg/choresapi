using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChoresApp.Models
{
    public class Family
    {
        [Key]
        public int FamilyId { get; set; }

        [Required]
        [MaxLength(100)] // Add constraints based on expected length
        public string? Name { get; set; }

        public virtual ICollection<User>? Users { get; set; } = new List<User>();

        public Family() { }
    }
}
