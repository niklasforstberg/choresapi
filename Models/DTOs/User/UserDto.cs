namespace ChoresApi.Models.DTOs.User;

public record UserDto(int Id, string Email, string Name, string Role, int? HouseholdId, string? Color);

// CurrentPassword is only checked when the email changes.
public record UpdateProfileDto(string Name, string Email, string? CurrentPassword);

// RefreshToken is the caller's own, if it has one: it stays valid while every other one is deleted.
public record ChangePasswordDto(string CurrentPassword, string NewPassword, string? RefreshToken = null);

public record DeleteAccountDto(string Password);

// Null goes back to whichever colour is free.
public record UpdateColorDto(string? Color);
