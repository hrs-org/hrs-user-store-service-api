using HRS.Domain.Entities;
using HRS.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<UserSession> UserSessions { get; set; } = default!;
    public DbSet<UserVerification> UserVerifications { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserSessionConfiguration());
        modelBuilder.ApplyConfiguration(new UserVerificationConfiguration());
    }
}
