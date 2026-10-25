using ChoresApi.Models.Enums;

namespace ChoresApi.Models;

public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? Name { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; } // last successful login; failed ones only go to the Security log
}
