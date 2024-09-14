using System.ComponentModel.DataAnnotations;

namespace ChoresApp.Models
{
    public class ChoreLog
    {
        [Key]
        public int Id { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime DueDate { get; set; }
        public int ChoreId { get; set; }
        public int UserId { get; set; }
        public virtual Chore? Chore { get; set; }
        public virtual ChoreUser? ChoreUser { get; set; }
    }
}