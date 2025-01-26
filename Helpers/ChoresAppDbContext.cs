using Microsoft.EntityFrameworkCore;
using ChoresApp.Models;
using Microsoft.EntityFrameworkCore.SqlServer;

namespace ChoresApp.Helpers
{
    public class ChoresAppDbContext : DbContext
    {
        public ChoresAppDbContext(DbContextOptions<ChoresAppDbContext> options) : base(options) { }

        public DbSet<Chore> Chores { get; set; }
        public DbSet<ChoreLog> ChoresLog { get; set; }
        public DbSet<ChoreUser> ChoreUsers { get; set; }
        public DbSet<Family> Families { get; set; }
        public DbSet<Invitation> Invitations { get; set; }

    }
}