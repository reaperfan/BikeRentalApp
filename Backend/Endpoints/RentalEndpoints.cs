using System.ComponentModel.DataAnnotations;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public static class RentalEndpoints {
    public static void MapRentalEndpoints(this IEndpointRouteBuilder routes) {
        var group = routes.MapGroup("/api/rentals").WithTags("Rentals");

        // Összes kölcsönzés listája
        group.MapGet("/", async (BikeDbContext db) => {
            var rentals = await db.Rentals
                .AsNoTracking()
                .Include(r => r.Bike)
                .ToListAsync();

            return Results.Ok(rentals.Select(r => new RentalDtoResponse(
                r.Id,
                r.BikeId,
                r.Bike?.Name ?? "Ismeretlen",
                r.CustomerName,
                r.CustomerEmail,
                r.StartDate,
                r.EndDate,
                r.CreatedAt
            )));
        })
        .AllowAnonymous()
        .WithName("GetRentals")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // Konkrét kölcsönzés lekérdezése ID alapján
        group.MapGet("/{id}", async (int id, BikeDbContext db) => {
            var rental = await db.Rentals
                .AsNoTracking()
                .Include(r => r.Bike)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental == null)
                return Results.NotFound(new { message = "Kölcsönzés nem található." });

            return Results.Ok(new RentalDtoResponse(
                rental.Id,
                rental.BikeId,
                rental.Bike?.Name ?? "Ismeretlen",
                rental.CustomerName,
                rental.CustomerEmail,
                rental.StartDate,
                rental.EndDate,
                rental.CreatedAt
            ));
        })
        .AllowAnonymous()
        .WithName("GetRentalById")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Kölcsönzés szűrése bicikli ID szerint
        group.MapGet("/bike/{bikeId}", async (int bikeId, BikeDbContext db) => {
            var rentals = await db.Rentals
                .AsNoTracking()
                .Where(r => r.BikeId == bikeId)
                .Include(r => r.Bike)
                .ToListAsync();

            return Results.Ok(rentals.Select(r => new RentalDtoResponse(
                r.Id,
                r.BikeId,
                r.Bike?.Name ?? "Ismeretlen",
                r.CustomerName,
                r.CustomerEmail,
                r.StartDate,
                r.EndDate,
                r.CreatedAt
            )));
        })
        .AllowAnonymous()
        .WithName("GetRentalsByBike")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // Új kölcsönzés létrehozása (POST) - csak bejelentkezett felhasználó
        group.MapPost("/", async (RentalDtoCreate dto, BikeDbContext db) => {
            // A DTO attribútumait a Minimal API nem ellenőrzi, ezért itt validálunk
            if (string.IsNullOrWhiteSpace(dto.CustomerName))
                return Results.BadRequest(new { message = "Az ügyfél neve kötelező." });

            if (!new EmailAddressAttribute().IsValid(dto.CustomerEmail))
                return Results.BadRequest(new { message = "Érvénytelen email cím." });

            // Bicikli létezésének ellenőrzése
            var bike = await db.Bikes.FirstOrDefaultAsync(b => b.Id == dto.BikeId);
            if (bike == null)
                return Results.BadRequest(new { message = "A megadott bicikli nem létezik." });

            // Dátumok validációja
            if (dto.StartDate >= dto.EndDate)
                return Results.BadRequest(new { message = "A kezdési dátum előbb kell legyen, mint a befejezési dátum." });

            if (dto.StartDate < DateTime.UtcNow)
                return Results.BadRequest(new { message = "A kezdési dátum nem lehet a múltban." });

            // Ütközések ellenőrzése (másik kölcsönzés már foglalta-e az időpontot)
            var conflict = await db.Rentals.AnyAsync(r =>
                r.BikeId == dto.BikeId &&
                r.StartDate < dto.EndDate &&
                r.EndDate > dto.StartDate
            );

            if (conflict)
                return Results.BadRequest(new { message = "Az adott bicikli már foglalt ebben az időszakban." });

            var rental = new Rental {
                BikeId = dto.BikeId,
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedAt = DateTime.UtcNow
            };

            db.Rentals.Add(rental);
            await db.SaveChangesAsync();

            return Results.Created($"/api/rentals/{rental.Id}", new RentalDtoResponse(
                rental.Id,
                rental.BikeId,
                bike.Name,
                rental.CustomerName,
                rental.CustomerEmail,
                rental.StartDate,
                rental.EndDate,
                rental.CreatedAt
            ));
        })
        .RequireAuthorization()
        .WithName("CreateRental")
        .WithOpenApi()
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        // Kölcsönzés módosítása (PUT)
        group.MapPut("/{id}", async (int id, RentalDtoUpdate dto, BikeDbContext db) => {
            var rental = await db.Rentals
                .Include(r => r.Bike)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental == null)
                return Results.NotFound(new { message = "Kölcsönzés nem található." });

            // Dátumok validációja
            if (dto.StartDate >= dto.EndDate)
                return Results.BadRequest(new { message = "A kezdési dátum előbb kell legyen, mint a befejezési dátum." });

            // Ütközések ellenőrzése (más kölcsönzéseket kizárva)
            var conflict = await db.Rentals.AnyAsync(r =>
                r.Id != id &&
                r.BikeId == rental.BikeId &&
                r.StartDate < dto.EndDate &&
                r.EndDate > dto.StartDate
            );

            if (conflict)
                return Results.BadRequest(new { message = "Az adott bicikli már foglalt ebben az új időszakban." });

            rental.StartDate = dto.StartDate;
            rental.EndDate = dto.EndDate;

            await db.SaveChangesAsync();

            return Results.Ok(new RentalDtoResponse(
                rental.Id,
                rental.BikeId,
                rental.Bike?.Name ?? "Ismeretlen",
                rental.CustomerName,
                rental.CustomerEmail,
                rental.StartDate,
                rental.EndDate,
                rental.CreatedAt
            ));
        })
        .AllowAnonymous()
        .WithName("UpdateRental")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // Kölcsönzés törlése (DELETE)
        group.MapDelete("/{id}", async (int id, BikeDbContext db) => {
            var rental = await db.Rentals.FirstOrDefaultAsync(r => r.Id == id);
            if (rental == null)
                return Results.NotFound(new { message = "Kölcsönzés nem található." });

            db.Rentals.Remove(rental);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .AllowAnonymous()
        .WithName("DeleteRental")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
