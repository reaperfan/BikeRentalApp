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
    }
}
