namespace ChoresApi.Models.DTOs
{
	public class UserDto
	{
        public required string Email { get; set; }
        public string? Password { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; } = "User";
        public int? FamilyId { get; set; }
        public FamilyDto? Family { get; set; }

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }

        
    }
}

