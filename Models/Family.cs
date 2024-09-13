using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChoresApp.Models
{
    public class Family
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public virtual ICollection<User>? Users { get; set; } = new List<User>();
        public Family() { }
    }
}
