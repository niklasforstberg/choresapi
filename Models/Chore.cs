using System.ComponentModel.DataAnnotations;

namespace ChoresApp.Models
{
    public class Chore
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

    }
}