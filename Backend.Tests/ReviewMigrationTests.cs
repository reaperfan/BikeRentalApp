using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Backend.Tests;

public sealed class ReviewMigrationTests
{
    [Theory]
    [InlineData("20261108205912_AddRentalsTable")]
    [InlineData("20261109120503_UniqueReviewPerAppointment")]
    [InlineData("20261109122908_OptionalReviewAppointment")]
    public async Task UpgradePreservesReviewsAndOtherTables_AndRemovesOldRelation(string previousVersion)
    {
        var path = Path.Combine(Path.GetTempPath(), $"review-migration-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<BikeDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
            await using var db = new BikeDbContext(options);
            await db.GetService<IMigrator>().MigrateAsync(previousVersion);
            var user = new User { Username = "Migráció", Email = "migration@example.com", PasswordHash = "not-used" };
            var appointment = new ServiceAppointment { User = user, Status = "Completed", BikeType = "MTB", ServiceType = "Szerviz", AppointmentDate = DateTime.UtcNow.AddDays(-1) };
            db.ServiceAppointments.Add(appointment);
            await db.SaveChangesAsync();
            var createdAt = DateTime.UtcNow.AddDays(-1);
            // Régi séma tesztadata: a korábbi kapcsolat csak az átállás előtt létezik.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Reviews (UserId, ServiceAppointmentId, Rating, Comment, CreatedAt)
                VALUES ({user.Id}, {appointment.Id}, {5}, {"Megőrzendő"}, {createdAt})
                """);
            var original = await db.Reviews.AsNoTracking().SingleAsync();
            var bikes = await db.Bikes.AsNoTracking().ToListAsync();
            var rental = new Rental { BikeId = bikes[0].Id, CustomerName = "Meglévő bérlő", CustomerEmail = "rental@example.com",
                StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
            db.Rentals.Add(rental);
            await db.SaveChangesAsync();
            if (previousVersion == "20261109122908_OptionalReviewAppointment")
            {
                db.Reviews.Add(new Review { UserId = user.Id, Rating = 4, Comment = "Korábbi önálló értékelés" });
                await db.SaveChangesAsync();
            }
            var previousCount = await db.Reviews.CountAsync();
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            var saved = await db.Reviews.AsNoTracking().SingleAsync(r => r.Id == original.Id);
            Assert.Equal(original.UserId, saved.UserId);
            Assert.Equal(original.Rating, saved.Rating);
            Assert.Equal(original.Comment, saved.Comment);
            Assert.Equal(original.CreatedAt, saved.CreatedAt);
            Assert.Equal(previousCount, await db.Reviews.CountAsync());
            Assert.Equal(bikes.Count, await db.Bikes.CountAsync());
            Assert.Contains(await db.Rentals.AsNoTracking().ToListAsync(), r => r.Id == rental.Id && r.CustomerEmail == rental.CustomerEmail);
            Assert.Contains(await db.ServiceAppointments.ToListAsync(), a => a.Id == appointment.Id);
            Assert.Contains(await db.Users.ToListAsync(), u => u.Id == user.Id);
            await ReviewSchemaTests.AssertIndependentTable(db);

            // A szerviz törlése már nem érintheti a korábban hozzá kapcsolt véleményt.
            await db.ServiceAppointments.Where(a => a.Id == appointment.Id).ExecuteDeleteAsync();
            Assert.Equal(previousCount, await db.Reviews.CountAsync());
            db.Reviews.AddRange(new Review { UserId = user.Id, Rating = 1 }, new Review { UserId = user.Id, Rating = 5 });
            await db.SaveChangesAsync();
            Assert.Equal(previousCount + 2, await db.Reviews.CountAsync());
        }
        finally { File.Delete(path); }
    }
}
