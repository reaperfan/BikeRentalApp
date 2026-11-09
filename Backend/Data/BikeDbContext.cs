using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class BikeDbContext : DbContext
{
    public BikeDbContext(DbContextOptions<BikeDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Bike> Bikes => Set<Bike>();
    public DbSet<Rental> Rentals => Set<Rental>();

    public DbSet<ServiceAppointment> ServiceAppointments => Set<ServiceAppointment>();

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // A korábbi szervizhez kötött értékelések kapcsolatát megőrizzük.
        // Új értékelésnél a kapcsolat null, ebből több is megengedett.
        modelBuilder.Entity<Review>()
            .HasIndex(r => r.ServiceAppointmentId)
            .IsUnique();

        // Rental - Bike kapcsolat (One-to-Many)
        modelBuilder.Entity<Rental>()
            .HasOne(r => r.Bike)
            .WithMany()
            .HasForeignKey(r => r.BikeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}