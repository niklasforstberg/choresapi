namespace ChoresApi.Models.DTOs.User;

public record UserDto(int Id, string Email, string Name, string Role, int? HouseholdId);

// CurrentPassword is only checked when the email changes.
public record UpdateProfileDto(string Name, string Email, string? CurrentPassword);

public record ChangePasswordDto(string CurrentPassword, string NewPassword);
