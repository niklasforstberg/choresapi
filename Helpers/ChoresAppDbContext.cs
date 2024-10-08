using Microsoft.EntityFrameworkCore;
using ChoresApp.Models;


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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChoreUser>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}