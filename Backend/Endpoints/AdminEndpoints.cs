using System.Security.Claims;
using Backend.Auth;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
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
        }).WithName("GetAdminStats");

        // Felhasználók
        group.MapGet("/users", async (BikeDbContext db, CancellationToken ct) =>
        {
            var users = await db.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync(ct);
            return Results.Ok(users.Select(u => new AdminUserResponse(u.Id, u.Username, u.Email, u.Role, u.CreatedAt)));
        }).WithName("GetAdminUsers");

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
        }).WithName("UpdateUserRole");

        // Kölcsönzések (szűrés: bicikli, időszak; a végösszeggel együtt)
        group.MapGet("/rentals", async (BikeDbContext db, int? bikeId, DateTime? from, DateTime? to, CancellationToken ct) =>
        {
            var query = db.Rentals.AsNoTracking().Include(r => r.Bike).AsQueryable();
            if (bikeId.HasValue) query = query.Where(r => r.BikeId == bikeId.Value);
            if (from.HasValue) query = query.Where(r => r.EndDate > from.Value);
            if (to.HasValue) query = query.Where(r => r.StartDate < to.Value);

            var rentals = await query.OrderByDescending(r => r.StartDate).ToListAsync(ct);
            return Results.Ok(rentals.Select(Response));
        }).WithName("GetAdminRentals");

        group.MapDelete("/rentals/{id:int}", async (int id, BikeDbContext db, CancellationToken ct) =>
        {
            var count = await db.Rentals.Where(r => r.Id == id).ExecuteDeleteAsync(ct);
            return count == 0 ? Results.NotFound(new { message = "Kölcsönzés nem található." }) : Results.NoContent();
        }).WithName("DeleteAdminRental");
    }
}
