namespace ChoresApi.Models;

/// <summary>
/// One member taking on a chore on a given day. Planned until CompletedAt is set.
/// A member may have several on the same chore and day.
/// </summary>
public class ChoreEntry
{
    public int Id { get; set; }
    public int ChoreId { get; set; }
    public Chore Chore { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
