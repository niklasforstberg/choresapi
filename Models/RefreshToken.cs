namespace ChoresApi.Models;

/// <summary>
/// Keeps an app logged in after its JWT expires. Only a SHA-256 hash of the token is
/// stored; each use replaces it with a new one, and logging out deletes it.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public required string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
