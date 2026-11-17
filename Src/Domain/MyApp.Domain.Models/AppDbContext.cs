using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Models.Models;

namespace MyApp.Domain.Models;

public class AppDbContext : DbContext
{
    public virtual DbSet<User> Users { get; set; }

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}