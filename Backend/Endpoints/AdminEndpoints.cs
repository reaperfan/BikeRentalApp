using System.Security.Claims;
using Backend.Auth;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Backend.Endpoints;

public static class AdminEndpoints
{
    private static int? UserId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    // A kosár ugyanígy számol: a két dátum közötti napok száma, legalább 1 nap.
    private static int Days(Rental r) => Math.Max(1, (int)Math.Round((r.EndDate - r.StartDate).TotalDays));

    private static AdminRentalResponse Response(Rental r) => new(r.Id, r.BikeId, r.Bike?.Name ?? "Ismeretlen",
        r.CustomerName, r.CustomerEmail, r.StartDate, r.EndDate, r.CreatedAt, Days(r), Days(r) * (r.Bike?.DailyPrice ?? 0));

    public static void MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization(AuthConstants.AdminPolicy);

        // Irányítópult összesítő
        group.MapGet("/stats", async (BikeDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            var now = clock.GetUtcNow().UtcDateTime;
            // SQLite nem tud decimal összeget számolni, ezért a bevétel memóriában készül.
            var rentals = await db.Rentals.AsNoTracking().Include(r => r.Bike).ToListAsync(ct);
            var ratings = await db.Reviews.AsNoTracking().Select(r => r.Rating).ToListAsync(ct);

            return Results.Ok(new AdminStatsResponse(
                await db.Bikes.CountAsync(ct),
                await db.Bikes.CountAsync(b => b.IsAvailable, ct),
                await db.Users.CountAsync(ct),
                await db.Users.CountAsync(u => u.Role == AuthConstants.AdminRole, ct),
                rentals.Count,
                rentals.Count(r => r.StartDate <= now && r.EndDate > now),
                rentals.Count(r => r.StartDate > now),
                rentals.Sum(r => Response(r).TotalPrice),
                await db.ServiceAppointments.CountAsync(a => a.Status == "Pending", ct),
                ratings.Count,
                ratings.Count == 0 ? null : ratings.Average()));
        }).WithName("GetAdminStats")
        .WithSummary("Admin összesítő")
        .WithDescription("Az irányítópult számai: kerékpárok (összes, elérhető), felhasználók (összes, admin), " +
            "kölcsönzések (összes, most futó, jövőbeli), teljes bevétel, függő szervizfoglalások, értékelések száma és átlaga. Csak admin.");

        // Felhasználók
        group.MapGet("/users", async (BikeDbContext db, CancellationToken ct) =>
        {
            var users = await db.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync(ct);
            return Results.Ok(users.Select(u => new AdminUserResponse(u.Id, u.Username, u.Email, u.Role, u.CreatedAt)));
        }).WithName("GetAdminUsers")
        .WithSummary("Felhasználók listája")
        .WithDescription("Az összes felhasználó azonosító szerint rendezve: név, e-mail, szerepkör, regisztráció ideje. Csak admin.");

        // A szerepkör-változás a felhasználó következő bejelentkezésekor kerül a tokenbe.
        group.MapPatch("/users/{id:int}/role", async (int id, UserRoleRequest request, ClaimsPrincipal principal,
            BikeDbContext db, CancellationToken ct) =>
        {
            if (request.Role is not (AuthConstants.AdminRole or AuthConstants.CustomerRole))
                return Results.BadRequest(new { message = "Engedélyezett szerepkör: Admin, Customer." });
            // Saját magát nem fokozhatja le, így mindig marad legalább egy admin.
            if (UserId(principal) == id && request.Role != AuthConstants.AdminRole)
                return Results.BadRequest(new { message = "A saját admin jogosultságodat nem veheted el." });

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user == null)
                return Results.NotFound(new { message = "Felhasználó nem található." });

            user.Role = request.Role;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new AdminUserResponse(user.Id, user.Username, user.Email, user.Role, user.CreatedAt));
        }).WithName("UpdateUserRole")
        .WithSummary("Szerepkör módosítása")
        .WithDescription("`Admin` vagy `Customer` szerepkört állít be. A saját admin jog nem vehető el (400), így mindig marad admin. " +
            "A változás a felhasználó következő bejelentkezésekor kerül a tokenbe. Nem létező felhasználóra 404. Csak admin.");

        // Kölcsönzések (szűrés: bicikli, időszak; a végösszeggel együtt)
        group.MapGet("/rentals", async (BikeDbContext db, int? bikeId, DateTime? from, DateTime? to, CancellationToken ct) =>
        {
            var query = db.Rentals.AsNoTracking().Include(r => r.Bike).AsQueryable();
            if (bikeId.HasValue) query = query.Where(r => r.BikeId == bikeId.Value);
            if (from.HasValue) query = query.Where(r => r.EndDate > from.Value);
            if (to.HasValue) query = query.Where(r => r.StartDate < to.Value);

            var rentals = await query.OrderByDescending(r => r.StartDate).ToListAsync(ct);
            return Results.Ok(rentals.Select(Response));
        }).WithName("GetAdminRentals")
        .WithSummary("Kölcsönzések napszámmal és végösszeggel")
        .WithDescription("Kölcsönzések a napok számával és a végösszeggel (napok × napidíj), a legkésőbb kezdődő elöl. " +
            "Opcionális szűrés: `bikeId`, valamint `from` és `to` (az időszakkal átfedő kölcsönzések). Csak admin.");

        // Bicikli kép feltöltése (multipart/form-data, "file" mező); a válasz URL-je mehet az ImageUrl-be
        group.MapPost("/bike-images", async (IFormFile? file, UploadStorage storage, CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { message = "Válassz ki egy képfájlt." });
            if (file.Length > UploadStorage.MaxImageBytes)
                return Results.BadRequest(new { message = "A kép legfeljebb 5 MB lehet." });

            await using var stream = file.OpenReadStream();
            var header = new byte[12];
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
            if (UploadStorage.DetectImageExtension(header.AsSpan(0, read)) is not string extension)
                return Results.BadRequest(new { message = "Csak JPG, PNG vagy WEBP kép tölthető fel." });

            stream.Position = 0;
            var url = await storage.SaveBikeImageAsync(stream, extension, ct);
            return Results.Created(url, new { url });
        })
        // JWT-vel hitelesítünk, nincs cookie, így az antiforgery token nem szükséges
        .DisableAntiforgery()
        .WithName("UploadBikeImage")
        .WithSummary("Kerékpárkép feltöltése")
        .WithDescription("Képfeltöltés `multipart/form-data` formában, `file` mezőben. JPG, PNG vagy WEBP, legfeljebb 5 MB; " +
            "a típust a fájl tartalmából ellenőrzi, nem a kiterjesztésből. A válaszban kapott `url` kerül a kerékpár " +
            "`imageUrl` mezőjébe. Hibára 400. Csak admin.");

        group.MapDelete("/rentals/{id:int}", async (int id, BikeDbContext db, CancellationToken ct) =>
        {
            var count = await db.Rentals.Where(r => r.Id == id).ExecuteDeleteAsync(ct);
            return count == 0 ? Results.NotFound(new { message = "Kölcsönzés nem található." }) : Results.NoContent();
        }).WithName("DeleteAdminRental")
        .WithSummary("Kölcsönzés törlése (admin)")
        .WithDescription("Bármely kölcsönzést törli. Nem létező kölcsönzésre 404. Csak admin.");
    }
}
