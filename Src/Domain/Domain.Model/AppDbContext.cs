using Domain.Model.Model;
using Microsoft.EntityFrameworkCore;

namespace Domain.Model;

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