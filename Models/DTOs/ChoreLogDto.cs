using System;

namespace ChoresApp.Models.DTOs
{
    public class ChoreLogDto
    {
        public int Id { get; set; }
        public DateTime? DueDate { get; set; }
        public int ChoreId { get; set; }
        public int UserId { get; set; }
        public required int ReportedByUserId { get; set; }
        public string? ChoreName { get; set; }
        public string? UserName { get; set; }
    }
}
