using HRS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Store> Stores { get; set; } = default!;
}
