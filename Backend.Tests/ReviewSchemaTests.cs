using Backend.Data;
using Backend.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public sealed class ReviewSchemaTests
{
    internal static async Task AssertIndependentTable(BikeDbContext db)
    {
        var columns = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Reviews') ORDER BY name").ToListAsync();
        Assert.Equal(new[] { "Comment", "CreatedAt", "Id", "Rating", "UserId" }, columns);
        var references = await db.Database.SqlQueryRaw<string>("SELECT \"table\" AS Value FROM pragma_foreign_key_list('Reviews')").ToListAsync();
        Assert.Equal("Users", Assert.Single(references));
        Assert.Empty(await db.Database.SqlQueryRaw<string>("SELECT \"table\" AS Value FROM pragma_foreign_key_check").ToListAsync());
        var indexes = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_index_list('Reviews')").ToListAsync();
        Assert.Equal("IX_Reviews_UserId", Assert.Single(indexes));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(6, 0)]
    [InlineData(5, 2001)]
    public async Task DatabaseAlsoRejectsInvalidReviewValues(int rating, int commentLength)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BikeDbContext>().UseSqlite(connection).Options;
        await using var db = new BikeDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var user = new User { Username = "Adatbázisteszt", Email = "db@example.com", PasswordHash = "not-used" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.Reviews.Add(new Review { UserId = user.Id, Rating = rating, Comment = new string('x', commentLength) });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(275, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
    }

    [Fact]
    public async Task FreshDatabaseHasNoServiceRelation_AndEnforcesReviewOwnership()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BikeDbContext>().UseSqlite(connection).Options;
        await using var db = new BikeDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await AssertIndependentTable(db);
        db.Reviews.Add(new Review { UserId = int.MaxValue, Rating = 5 });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(787, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
        db.ChangeTracker.Clear();
        var owner = new User { Username = "Szerző", Email = "owner@example.com", PasswordHash = "not-used" };
        var other = new User { Username = "Másik", Email = "other@example.com", PasswordHash = "not-used" };
        db.Reviews.AddRange(new Review { User = owner, Rating = 5 }, new Review { User = other, Rating = 1 });
        await db.SaveChangesAsync();
        await db.Users.Where(u => u.Id == owner.Id).ExecuteDeleteAsync();
        Assert.Equal(other.Id, (await db.Reviews.SingleAsync()).UserId);
        Assert.True(await db.Users.AnyAsync(u => u.Id == other.Id));
    }
}
