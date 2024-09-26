using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChoresApp.Models
{
    public class Family
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual ICollection<ChoreUser>? ChoreUsers { get; set; } = new List<ChoreUser>();
        public Family() { }
    }
}
