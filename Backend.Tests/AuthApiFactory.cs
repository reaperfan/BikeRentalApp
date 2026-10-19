using Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests;

public class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "test-signing-key-that-is-long-enough-1234567890";
    public const string Issuer = "BikeRental.Api";
    public const string Audience = "BikeRental.Client";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"bikerental-test-{Guid.NewGuid()}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("Jwt:Key", Key);
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
    }

    protected override void ConfigureClient(HttpClient client)
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<BikeDbContext>().Database.EnsureCreated();
        base.ConfigureClient(client);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
    }
}
