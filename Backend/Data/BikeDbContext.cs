using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class BikeDbContext : DbContext
{
    public BikeDbContext(DbContextOptions<BikeDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    
}