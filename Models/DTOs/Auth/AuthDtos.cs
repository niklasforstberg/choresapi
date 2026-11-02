namespace ChoresApi.Models.DTOs.Auth;

// An app that should stay logged in asks for a refresh token; the web app doesn't.
public record LoginDto(string Email, string Password, bool RefreshToken = false);

public record RegisterDto(string Email, string Password, string Name, bool RefreshToken = false);

public record RefreshDto(string RefreshToken);

public record TokenResponse(string Token, string? RefreshToken = null);
