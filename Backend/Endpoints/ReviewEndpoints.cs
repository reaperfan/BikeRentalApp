using System.Security.Claims;
using Backend.Auth;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Backend.Endpoints;

public static class ReviewEndpoints
{
    private static int? UserId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) && id > 0 ? id : null;
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static OwnReviewResponse Own(Review review) =>
        new(review.Id, review.Rating, review.Comment, Utc(review.CreatedAt));
    private static bool Valid(int rating, string? comment) => rating is >= 1 and <= 5 && (comment?.Length ?? 0) <= 2000;
    private static string? Normalize(string? comment) => string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    private static IResult Invalid() => Results.BadRequest(new { message = "A pontszám 1 és 5 közötti egész szám, a vélemény legfeljebb 2000 karakter lehet." });

    public static void MapReviewEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/reviews").WithTags("Értékelések").RequireAuthorization();
        group.MapGet("/", async (BikeDbContext db, int? page, int? pageSize, int? rating, CancellationToken ct) =>
        {
            var number = page ?? 1;
            var size = pageSize ?? 10;
            // A felső korlát a Skip szorzás túlcsordulását is megakadályozza.
            if (number is < 1 or > 1_000_000 || size is < 1 or > 100 || rating is < 1 or > 5)
                return Results.BadRequest(new { message = "Az oldal 1–1000000, az oldalméret 1–100, a pontszám 1–5 lehet." });
            var query = db.Reviews.AsNoTracking();
            if (rating.HasValue) query = query.Where(r => r.Rating == rating.Value);
            var total = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
                .Skip((number - 1) * size).Take(size)
                .Select(r => new { r.Id, AuthorName = r.User.Username, r.Rating, r.Comment, r.CreatedAt }).ToListAsync(ct);
            return Results.Ok(new ReviewPage(number, size, total,
                rows.Select(r => new PublicReviewResponse(r.Id, r.AuthorName, r.Rating, r.Comment, Utc(r.CreatedAt))).ToList()));
        }).AllowAnonymous().WithName("GetReviews")
        .WithSummary("Értékelések listája")
        .WithDescription("Lapozott lista, a legújabb elöl, a szerző felhasználónevével. Paraméterek: `page` (alapértelmezés 1), " +
            "`pageSize` (alapértelmezés 10, legfeljebb 100), `rating` (szűrés 1–5 pontra). Hibás paraméterre 400. Bejelentkezés nem kell.");

        group.MapGet("/summary", async (BikeDbContext db, CancellationToken ct) =>
        {
            var counts = await db.Reviews.AsNoTracking().GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() }).ToDictionaryAsync(r => r.Rating, r => r.Count, ct);
            var total = counts.Values.Sum();
            return Results.Ok(new ReviewSummary(total,
                total == 0 ? null : counts.Sum(r => (double)r.Key * r.Value) / total,
                counts.GetValueOrDefault(1), counts.GetValueOrDefault(2), counts.GetValueOrDefault(3),
                counts.GetValueOrDefault(4), counts.GetValueOrDefault(5)));
        }).AllowAnonymous().WithName("GetReviewSummary")
        .WithSummary("Értékelések összesítője")
        .WithDescription("Az értékelések száma, átlaga és pontszámonkénti (1–5) eloszlása. Értékelés nélkül az átlag null. Bejelentkezés nem kell.");

        group.MapGet("/mine", async (ClaimsPrincipal user, BikeDbContext db, CancellationToken ct) =>
        {
            if (UserId(user) is not int userId) return Results.Unauthorized();
            var reviews = await db.Reviews.AsNoTracking().Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).ToListAsync(ct);
            return Results.Ok(reviews.Select(Own));
        }).WithName("GetMyReviews")
        .WithSummary("Saját értékeléseim")
        .WithDescription("A bejelentkezett felhasználó értékelései, a legújabb elöl. Token nélkül 401.");

        group.MapPost("/", async (CreateReviewRequest request, ClaimsPrincipal user, BikeDbContext db,
            TimeProvider clock, CancellationToken ct) =>
        {
            if (UserId(user) is not int userId) return Results.Unauthorized();
            if (!Valid(request.Rating, request.Comment)) return Invalid();
            if (!await db.Users.AnyAsync(u => u.Id == userId, ct)) return Results.Unauthorized();
            var review = new Review { UserId = userId, Rating = request.Rating,
                Comment = Normalize(request.Comment), CreatedAt = clock.GetUtcNow().UtcDateTime };
            db.Reviews.Add(review);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/reviews/{review.Id}", Own(review));
        }).WithName("CreateReview")
        .WithSummary("Értékelés írása")
        .WithDescription("Új értékelés 1–5 pontszámmal és opcionális, legfeljebb 2000 karakteres véleménnyel. " +
            "Hibás adatra 400, token nélkül 401.");

        group.MapGet("/{id:int}", async (int id, BikeDbContext db, CancellationToken ct) =>
        {
            var row = await db.Reviews.AsNoTracking().Where(r => r.Id == id)
                .Select(r => new { r.Id, AuthorName = r.User.Username, r.Rating, r.Comment, r.CreatedAt }).SingleOrDefaultAsync(ct);
            return row == null ? Results.NotFound(new { message = "Az értékelés nem található." }) : Results.Ok(
                new PublicReviewResponse(row.Id, row.AuthorName, row.Rating, row.Comment, Utc(row.CreatedAt)));
        }).AllowAnonymous().WithName("GetReview")
        .WithSummary("Értékelés adatai")
        .WithDescription("Egy értékelés azonosító alapján, a szerző felhasználónevével. Nem létezőre 404. Bejelentkezés nem kell.");

        group.MapPut("/{id:int}", async (int id, UpdateReviewRequest request, ClaimsPrincipal user, BikeDbContext db, CancellationToken ct) =>
        {
            if (UserId(user) is not int userId) return Results.Unauthorized();
            if (!Valid(request.Rating, request.Comment)) return Invalid();
            var comment = Normalize(request.Comment);
            // Feltételes UPDATE: a tulajdonos ellenőrzése és a módosítás atomi.
            var count = await db.Reviews.Where(r => r.Id == id && r.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Rating, request.Rating).SetProperty(r => r.Comment, comment), ct);
            return count == 0 ? Results.NotFound(new { message = "Az értékelés nem található." }) : Results.NoContent();
        }).WithName("UpdateMyReview")
        .WithSummary("Saját értékelés módosítása")
        .WithDescription("A pontszámot és a véleményt módosítja. Csak a saját értékelés módosítható: másé vagy nem létező esetén 404. " +
            "Hibás adatra 400.");

        group.MapDelete("/{id:int}", async (int id, ClaimsPrincipal user, BikeDbContext db, CancellationToken ct) =>
        {
            if (UserId(user) is not int userId) return Results.Unauthorized();
            var count = await db.Reviews.Where(r => r.Id == id && r.UserId == userId).ExecuteDeleteAsync(ct);
            return count == 0 ? Results.NotFound(new { message = "Az értékelés nem található." }) : Results.NoContent();
        }).WithName("DeleteMyReview")
        .WithSummary("Saját értékelés törlése")
        .WithDescription("Csak a saját értékelés törölhető: másé vagy nem létező esetén 404.");

        group.MapDelete("/admin/{id:int}", async (int id, BikeDbContext db, CancellationToken ct) =>
        {
            var count = await db.Reviews.Where(r => r.Id == id).ExecuteDeleteAsync(ct);
            return count == 0 ? Results.NotFound(new { message = "Az értékelés nem található." }) : Results.NoContent();
        }).RequireAuthorization(AuthConstants.AdminPolicy).WithName("ModerateReview")
        .WithSummary("Értékelés moderálása")
        .WithDescription("Bármely értékelést törli (például sértő tartalom miatt). Nem létezőre 404. Csak admin.");
    }
}
