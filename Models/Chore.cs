namespace ChoresApi.Models;

public class Chore
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public Household Household { get; set; } = null!;
    public required string Name { get; set; }

    /// <summary>
    /// How many times each member should do this per week. Null for chores
    /// done when needed, like taking out the rubbish.
    /// </summary>
    public int? WeeklyTarget { get; set; }

    public int SortOrder { get; set; }

    public ICollection<ChoreEntry> Entries { get; set; } = [];
}
