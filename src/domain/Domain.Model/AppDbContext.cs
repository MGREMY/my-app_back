using Domain.Model.Model;
using Microsoft.EntityFrameworkCore;

namespace Domain.Model;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
}