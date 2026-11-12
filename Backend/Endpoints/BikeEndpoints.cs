using Backend.Auth;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public static class BikeEndpoints
{
    // A DTO-k DataAnnotations attribútumait a Minimal API nem ellenőrzi, ezért itt validálunk.
    private static IResult? Validate(string name, BikeType type, decimal dailyPrice, string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100 || !Enum.IsDefined(type) ||
            dailyPrice is <= 0 or > 999999.99m || string.IsNullOrWhiteSpace(imageUrl))
            return Results.BadRequest(new { message = "A név (max. 100 karakter), a típus, a pozitív napidíj és a kép elérési útja kötelező." });
        return null;
    }

    public static void MapBikeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/bikes").WithTags("Bikes").AllowAnonymous();

        // Összes bicikli listája (szűréssel: type, available, vagy kombinált)
        group.MapGet("/", async (BikeDbContext db, BikeType? type, bool? available) =>
        {
            IQueryable<Bike> query = db.Bikes.AsNoTracking();

            if (type.HasValue)
                query = query.Where(b => b.Type == type.Value);

            if (available.HasValue)
                query = query.Where(b => b.IsAvailable == available.Value);

            var bikes = await query.ToListAsync();
            return Results.Ok(bikes.Select(b => new BikeDtoResponse(
                b.Id, b.Name, b.Type, b.DailyPrice, b.ImageUrl, b.IsAvailable)));
        })
        .WithName("GetBikes")
        .WithOpenApi()
        .WithSummary("Kerékpárok listája")
        .WithDescription("Az összes kerékpár. Opcionális szűrés: `type` (kerékpártípus), `available` (true: csak a bérelhetők). " +
            "Ebből épül a katalógus. Bejelentkezés nem kell.")
        .Produces(StatusCodes.Status200OK);

        // Konkrét bicikli lekérdezése ID alapján
        group.MapGet("/{id}", async (int id, BikeDbContext db) =>
        {
            var bike = await db.Bikes.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (bike == null)
                return Results.NotFound(new { message = "Bicikli nem található." });

            return Results.Ok(new BikeDtoResponse(
                bike.Id, bike.Name, bike.Type, bike.DailyPrice, bike.ImageUrl, bike.IsAvailable));
        })
        .WithName("GetBikeById")
        .WithOpenApi()
        .WithSummary("Kerékpár adatai")
        .WithDescription("Egy kerékpár adatai azonosító alapján. Nem létező azonosítóra 404. Bejelentkezés nem kell.");

        // Admin operációk
        var adminGroup = routes.MapGroup("/api/bikes").WithTags("Bikes Admin").RequireAuthorization(AuthConstants.AdminPolicy);

        // Új bicikli létrehozása (POST)
        adminGroup.MapPost("/", async (BikeDtoCreateUpdate dto, BikeDbContext db) =>
        {
            if (Validate(dto.Name, dto.Type, dto.DailyPrice, dto.ImageUrl) is { } invalid)
                return invalid;

            var bike = new Bike
            {
                Name = dto.Name.Trim(),
                Type = dto.Type,
                DailyPrice = dto.DailyPrice,
                ImageUrl = dto.ImageUrl,
                IsAvailable = true
            };

            db.Bikes.Add(bike);
            await db.SaveChangesAsync();

            return Results.Created($"/api/bikes/{bike.Id}", new BikeDtoResponse(
                bike.Id, bike.Name, bike.Type, bike.DailyPrice, bike.ImageUrl, bike.IsAvailable));
        })
        .WithName("CreateBike")
        .WithOpenApi()
        .WithSummary("Kerékpár felvétele")
        .WithDescription("Új kerékpárt hoz létre, elérhető állapotban. Kötelező: név (legfeljebb 100 karakter), típus, " +
            "pozitív napidíj és kép URL (ezt a `POST /api/admin/bike-images` adja vissza). Hibás adatra 400. Csak admin.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // Bicikli módosítása (PUT)
        adminGroup.MapPut("/{id}", async (int id, BikeDtoUpdateFull dto, BikeDbContext db) =>
        {
            if (Validate(dto.Name, dto.Type, dto.DailyPrice, dto.ImageUrl) is { } invalid)
                return invalid;

            var bike = await db.Bikes.FirstOrDefaultAsync(b => b.Id == id);
            if (bike == null)
                return Results.NotFound(new { message = "Bicikli nem található." });

            bike.Name = dto.Name.Trim();
            bike.Type = dto.Type;
            bike.DailyPrice = dto.DailyPrice;
            bike.ImageUrl = dto.ImageUrl;
            bike.IsAvailable = dto.IsAvailable;

            await db.SaveChangesAsync();

            return Results.Ok(new BikeDtoResponse(
                bike.Id, bike.Name, bike.Type, bike.DailyPrice, bike.ImageUrl, bike.IsAvailable));
        })
        .WithName("UpdateBike")
        .WithOpenApi()
        .WithSummary("Kerékpár módosítása")
        .WithDescription("A kerékpár összes adatát felülírja, az elérhetőséget is (`isAvailable: false` letiltja, " +
            "nem lesz bérelhető). Hibás adatra 400, nem létező kerékpárra 404. Csak admin.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // Bicikli törlése (DELETE)
        adminGroup.MapDelete("/{id}", async (int id, BikeDbContext db) =>
        {
            var bike = await db.Bikes.FirstOrDefaultAsync(b => b.Id == id);
            if (bike == null)
                return Results.NotFound(new { message = "Bicikli nem található." });

            // A kölcsönzések Restrict kapcsolattal hivatkoznak rá, törlés helyett elérhetetlenné kell tenni.
            if (await db.Rentals.AnyAsync(r => r.BikeId == id))
                return Results.Conflict(new { message = "A biciklihez kölcsönzés tartozik, nem törölhető. Állítsd nem elérhetőre." });

            db.Bikes.Remove(bike);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteBike")
        .WithOpenApi()
        .WithSummary("Kerékpár törlése")
        .WithDescription("Véglegesen törli a kerékpárt. Ha kölcsönzés tartozik hozzá, nem törölhető (409): " +
            "ilyenkor módosítással állítsd nem elérhetőre. Nem létező kerékpárra 404. Csak admin.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}
