using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Backend.Tests;

public sealed class ReviewStartupTests
{
    private sealed class DevelopmentFactory(string path) : AuthApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={path};Pooling=False");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartupUpgradesOldDatabase_AndReviewSubmissionSucceeds(bool legacyMigrationNames)
    {
        var path = Path.Combine(Path.GetTempPath(), $"review-startup-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<BikeDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
            await using (var db = new BikeDbContext(options))
            {
                await db.GetService<IMigrator>().MigrateAsync("20261109120503_UniqueReviewPerAppointment");
                var user = new User { Username = "Korábbi ügyfél", Email = "old@example.com", PasswordHash = "not-used" };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                db.Reviews.Add(new Review { UserId = user.Id, Rating = 5 });
                var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
                Assert.Equal(1299, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
                if (legacyMigrationNames)
                {
                    foreach (var name in new[] { "ServiceAppointments", "Reviews" })
                    {
                        var prefix = name == "Reviews" ? "20261101183543_" : "20261101183220_";
                        var canonical = prefix + name;
                        var legacy = prefix + "\u2060" + name;
                        await db.Database.ExecuteSqlInterpolatedAsync(
                            $"UPDATE \"__EFMigrationsHistory\" SET \"MigrationId\"={legacy} WHERE \"MigrationId\"={canonical}");
                    }
                }
            }
            using var factory = new DevelopmentFactory(path);
            using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            var email = $"startup-{Guid.NewGuid():N}@example.com";
            (await client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto("Új értékelő", email, "Password123!"))).EnsureSuccessStatusCode();
            var login = await client.PostAsJsonAsync("/api/auth/login", new UserLoginDto(email, "Password123!"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!.AccessToken);
            var response = await client.PostAsJsonAsync("/api/reviews", new CreateReviewRequest(5, "Migráció után működik."));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            await using var check = new BikeDbContext(options);
            Assert.Empty(await check.Database.GetPendingMigrationsAsync());
            await ReviewSchemaTests.AssertIndependentTable(check);
            Assert.Contains(await check.Users.ToListAsync(), u => u.Email == "old@example.com");
            Assert.Equal("Migráció után működik.", (await check.Reviews.SingleAsync()).Comment);
            // Ismételt inicializálás nem változtatja meg az adatokat.
            await DevelopmentDatabase.InitializeAsync(check);
            Assert.Equal(1, await check.Reviews.CountAsync());
        }
        finally { File.Delete(path); }
    }
}
