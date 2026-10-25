namespace ChoresApi.Models.DTOs.Auth;

public record LoginDto(string Email, string Password);

public record RegisterDto(string Email, string Password, string? Name);

public record TokenResponse(string Token);
