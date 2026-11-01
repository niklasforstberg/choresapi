namespace ChoresApi.Models;

/// <summary>
/// One member taking on a chore for a given day, today or later. Removed once it
/// is done (it becomes a ChoreCompletion) or when the member drops it.
/// A member may have several on the same chore and day.
/// </summary>
public class ChoreClaim
{
    public int Id { get; set; }
    public int ChoreId { get; set; }
    public Chore Chore { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; }
}
