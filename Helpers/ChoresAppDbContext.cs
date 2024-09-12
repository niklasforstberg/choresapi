using Microsoft.EntityFrameworkCore;
using ChoresApp.Models;


namespace ChoresApp.Helpers
{
    public class ChoresAppDbContext : DbContext
    {
        public ChoresAppDbContext(DbContextOptions<ChoresAppDbContext> options) : base(options) { }

        public DbSet<Chore> Chores { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
    }
}