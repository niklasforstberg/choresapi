namespace ChoresApi.Models;

/// <summary>
/// A chore a member has done on a given day, today or earlier. Date is the day it
/// counts for, which may be before CreatedAt when it is logged after the fact.
/// Any member may record it for another, so RecordedBy can differ from User.
/// </summary>
public class ChoreCompletion
{
    public int Id { get; set; }
    public int ChoreId { get; set; }
    public Chore Chore { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int RecordedById { get; set; }
    public User RecordedBy { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; }
}
