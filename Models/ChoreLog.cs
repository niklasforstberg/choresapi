using System.ComponentModel.DataAnnotations;

namespace ChoresApp.Models
{
    public class ChoreLog
    {
        [Key]
        public int Id { get; set; }
        public DateTime? DueDate { get; set; }
        public int ChoreId { get; set; }
        public int UserId { get; set; }
        public int ReportedByUserId { get; set; }
        public virtual required Chore Chore { get; set; }
        public virtual required ChoreUser ChoreUser { get; set; }
    }
}