using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Backend.Tests;

public sealed class ReviewMigrationTests
{
    [Fact]
    public async Task MigrationPreservesExistingReviewsAndAllowsReviewsWithoutService()
    {
        var path = Path.Combine(Path.GetTempPath(), $"review-migration-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<BikeDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
            await using var db = new BikeDbContext(options);
            await db.GetService<IMigrator>().MigrateAsync("20261108205912_AddRentalsTable");
            var user = new User { Username = "Migráció", Email = "migration@example.com", PasswordHash = "not-used" };
            var appointment = new ServiceAppointment { User = user, Status = "Completed", BikeType = "MTB", ServiceType = "Szerviz", AppointmentDate = DateTime.UtcNow.AddDays(-1) };
            var review = new Review { User = user, ServiceAppointment = appointment, Rating = 5, Comment = "Megőrzendő" };
            db.Reviews.Add(review);
            await db.SaveChangesAsync();
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            Assert.False(db.Database.HasPendingModelChanges());
            var saved = await db.Reviews.AsNoTracking().SingleAsync();
            Assert.Equal("Megőrzendő", saved.Comment);
            Assert.Equal(review.Id, saved.Id);
            Assert.Equal(appointment.Id, saved.ServiceAppointmentId);
            db.Reviews.AddRange(
                new Review { UserId = user.Id, Rating = 1 },
                new Review { UserId = user.Id, Rating = 5 });
            await db.SaveChangesAsync();
            Assert.Equal(3, await db.Reviews.CountAsync());
            Assert.Equal(2, await db.Reviews.CountAsync(r => r.ServiceAppointmentId == null));
        }
        finally { File.Delete(path); }
    }
}
