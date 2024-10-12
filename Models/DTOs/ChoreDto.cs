namespace ChoresApi.Models.DTOs
{
    public class ChoreDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? FamilyId { get; set; }
    }
}
