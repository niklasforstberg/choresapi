namespace ChoresApi.Models.DTOs
{
    public class FamilyDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}