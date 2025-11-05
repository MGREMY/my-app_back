using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Model.Model;

namespace MyApp.Domain.Model;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
}