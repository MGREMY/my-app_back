using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Model.Model;

namespace MyApp.Domain.Model;

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