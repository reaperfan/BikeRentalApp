using Backend.Auth;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public static class BikeEndpoints
{
    public static void MapBikeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/bikes").WithTags("Bikes").AllowAnonymous();

        // Összes bicikli listája
        group.MapGet("/", async (BikeDbContext db) =>
        {
            var bikes = await db.Bikes.AsNoTracking().ToListAsync();
            return Results.Ok(bikes.Select(b => new BikeDtoResponse(
                b.Id, b.Name, b.Type, b.DailyPrice, b.ImageUrl, b.IsAvailable)));
        });

        // Konkrét bicikli lekérdezése ID alapján
        group.MapGet("/{id}", async (int id, BikeDbContext db) =>
        {
            var bike = await db.Bikes.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (bike == null)
                return Results.NotFound(new { message = "Bicikli nem található." });

            return Results.Ok(new BikeDtoResponse(
                bike.Id, bike.Name, bike.Type, bike.DailyPrice, bike.ImageUrl, bike.IsAvailable));
        });

        // Admin operációk
        var adminGroup = routes.MapGroup("/api/bikes").WithTags("Bikes Admin").RequireAuthorization(AuthConstants.AdminPolicy);

        // Új bicikli létrehozása (POST)
        adminGroup.MapPost("/", async (BikeDtoCreateUpdate dto, BikeDbContext db) =>
        {
            var bike = new Bike
            {
                Name = dto.Name,
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
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // Bicikli módosítása (PUT)
        adminGroup.MapPut("/{id}", async (int id, BikeDtoUpdateFull dto, BikeDbContext db) =>
        {
            var bike = await db.Bikes.FirstOrDefaultAsync(b => b.Id == id);
            if (bike == null)
                return Results.NotFound(new { message = "Bicikli nem található." });

            bike.Name = dto.Name;
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
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // Bicikli törlése (DELETE)
        adminGroup.MapDelete("/{id}", async (int id, BikeDbContext db) =>
        {
            var bike = await db.Bikes.FirstOrDefaultAsync(b => b.Id == id);
            if (bike == null)
                return Results.NotFound(new { message = "Bicikli nem található." });

            db.Bikes.Remove(bike);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteBike")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
