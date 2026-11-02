using Microsoft.EntityFrameworkCore;
using ChoresApi.Models;

namespace ChoresApi.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<Chore> Chores => Set<Chore>();
    public DbSet<ChoreClaim> ChoreClaims => Set<ChoreClaim>();
    public DbSet<ChoreCompletion> ChoreCompletions => Set<ChoreCompletion>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.Name).HasMaxLength(100);
            entity.Property(u => u.Color).HasMaxLength(16);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(u => u.Household)
                .WithMany(h => h.Members)
                .HasForeignKey(u => u.HouseholdId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Household>(entity =>
        {
            entity.HasIndex(h => h.InviteCode).IsUnique();
            entity.Property(h => h.Name).HasMaxLength(100);
            entity.Property(h => h.InviteCode).HasMaxLength(16);
            entity.Property(h => h.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Chore>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(100);
            entity.HasOne(c => c.Household)
                .WithMany(h => h.Chores)
                .HasForeignKey(c => c.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChoreClaim>(entity =>
        {
            entity.HasIndex(e => new { e.ChoreId, e.Date });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.Chore)
                .WithMany(c => c.Claims)
                .HasForeignKey(e => e.ChoreId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChoreCompletion>(entity =>
        {
            entity.HasIndex(e => new { e.ChoreId, e.Date });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.Chore)
                .WithMany(c => c.Completions)
                .HasForeignKey(e => e.ChoreId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RecordedBy)
                .WithMany()
                .HasForeignKey(e => e.RecordedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.Property(t => t.TokenHash).HasMaxLength(64);
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
