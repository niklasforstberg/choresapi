using ChoresApi.Models.Enums;

namespace ChoresApi.Models;

public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Name { get; set; } // its first letter marks the user's entries in the week grid
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; } // last successful login; failed ones only go to the Security log

    // Null until the user creates or joins one; a user is in at most one household.
    public int? HouseholdId { get; set; }
    public Household? Household { get; set; }
}
