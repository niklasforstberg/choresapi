namespace ChoresApi.Models;

public class Household
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string InviteCode { get; set; } // shared out of band; anyone who has it may join
    public DateTime CreatedAt { get; set; }

    public ICollection<User> Members { get; set; } = [];
    public ICollection<Chore> Chores { get; set; } = [];
}
