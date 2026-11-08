using Backend.Data;
using Backend.Models;
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
        var db = scope.ServiceProvider.GetRequiredService<BikeDbContext>();
        db.Database.EnsureCreated();

        // Seed bikes ha még nincsenek
        if (!db.Bikes.Any())
        {
            db.Bikes.AddRange(
                new Bike { Name = "Trek Marlin 7", Type = BikeType.MTB, DailyPrice = 8000, ImageUrl = "/images/bikes/BikeRental_Trek_Martin_7.jpg", IsAvailable = true },
                new Bike { Name = "Cube Aim Pro", Type = BikeType.MTB, DailyPrice = 7500, ImageUrl = "/images/bikes/BikeRental_Cube_Aim_Pro.jpg", IsAvailable = true },
                new Bike { Name = "Merida Big Nine 200", Type = BikeType.MTB, DailyPrice = 8500, ImageUrl = "/images/bikes/BikeRental_Merida_Big_Nine_200.jpg", IsAvailable = true },
                new Bike { Name = "Trek Dual Sport 3", Type = BikeType.Trekking, DailyPrice = 7000, ImageUrl = "/images/bikes/BikeRental_Trek_Dual_Sport_3.jpg", IsAvailable = true },
                new Bike { Name = "Merida Crossway 20", Type = BikeType.Trekking, DailyPrice = 6500, ImageUrl = "/images/bikes/BikeRental_Merida_Crossway_20.jpg", IsAvailable = true },
                new Bike { Name = "Cube Nature Pro", Type = BikeType.Trekking, DailyPrice = 7500, ImageUrl = "/images/bikes/BikeRental_Cube_Nature_Pro.jpg", IsAvailable = true },
                new Bike { Name = "Csepel Royal", Type = BikeType.City, DailyPrice = 4500, ImageUrl = "/images/bikes/BikeRental_Csepel_Royal.jpg", IsAvailable = true },
                new Bike { Name = "Neuzer City Life", Type = BikeType.City, DailyPrice = 5000, ImageUrl = "/images/bikes/BikeRental_Neuzer_City_Life.jpg", IsAvailable = true },
                new Bike { Name = "Kellys Classic Urban", Type = BikeType.City, DailyPrice = 5500, ImageUrl = "/images/bikes/BikeRental_Kellys_Classic_Urban.jpg", IsAvailable = true },
                new Bike { Name = "Cube Kathmandu Hybrid", Type = BikeType.EBike, DailyPrice = 14000, ImageUrl = "/images/bikes/BikeRental_Cube_Kathmandu_Hybrid.jpg", IsAvailable = true },
                new Bike { Name = "Specialized Turbo Vado", Type = BikeType.EBike, DailyPrice = 15000, ImageUrl = "/images/bikes/BikeRental_Specialized_Turbo_Vado.jpg", IsAvailable = true },
                new Bike { Name = "Haibike Trekking 4", Type = BikeType.EBike, DailyPrice = 14500, ImageUrl = "/images/bikes/BikeRental_Haibike_Trekking_4.jpg", IsAvailable = true },
                new Bike { Name = "Cube Kid 200", Type = BikeType.Kid, DailyPrice = 3000, ImageUrl = "/images/bikes/BikeRental_Cube_Kid_200.jpg", IsAvailable = true },
                new Bike { Name = "Merida Matts J24", Type = BikeType.Kid, DailyPrice = 3200, ImageUrl = "/images/bikes/BikeRental_Merida_Matts_J24.jpg", IsAvailable = true },
                new Bike { Name = "Gepida Junior 24", Type = BikeType.Kid, DailyPrice = 2800, ImageUrl = "/images/bikes/BikeRental_Gepida_Junior_24.jpg", IsAvailable = true }
            );
            db.SaveChanges();
        }

        base.ConfigureClient(client);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
    }
}
