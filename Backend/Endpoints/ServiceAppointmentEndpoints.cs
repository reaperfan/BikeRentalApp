using System.Security.Claims;
using Backend.Auth;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Backend.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Backend.Endpoints;

public static class ServiceAppointmentEndpoints
{
    private static ServiceAppointmentResponse Response(ServiceAppointment a) => new(a.Id,
        new DateTimeOffset(DateTime.SpecifyKind(a.AppointmentDate, DateTimeKind.Utc)), a.BikeType,
        a.ServiceType, a.ProblemDescription, a.Status,
        new DateTimeOffset(DateTime.SpecifyKind(a.CreatedAt, DateTimeKind.Utc)));
    private static int? UserId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
    private static IResult Error(string message) => Results.BadRequest(new { message });

    public static void MapServiceAppointmentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/service-appointments").RequireAuthorization().WithTags("Szerviz");
        group.MapGet("/availability", async (DateOnly from, DateOnly to, BikeDbContext db,
            IOptions<ServiceSchedule> options, TimeProvider clock, CancellationToken ct) =>
        {
            var schedule = options.Value;
            var now = clock.GetUtcNow().UtcDateTime;
            if (from < schedule.Today(now) || to < from || to.DayNumber - from.DayNumber > 30 ||
                to > schedule.Today(now).AddDays(schedule.BookingHorizonDays))
                return Error("Legfeljebb 31 nap kérhető le a foglalási időszakon belül.");
            var slots = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
                .SelectMany(i => schedule.Slots(from.AddDays(i))).Where(s => s > now).ToArray();
            var min = slots.FirstOrDefault();
            var max = slots.LastOrDefault();
            var occupied = await db.ServiceAppointments.AsNoTracking()
                .Where(a => a.Status != "Cancelled" && a.AppointmentDate >= min && a.AppointmentDate <= max)
                .Select(a => a.AppointmentDate).ToListAsync(ct);
            var busy = occupied.ToHashSet();
            return Results.Ok(new { timeZone = schedule.TimeZoneId, slotMinutes = schedule.SlotMinutes,
                slots = slots.Select(s => new { startsAt = new DateTimeOffset(s),
                    endsAt = new DateTimeOffset(s.AddMinutes(schedule.SlotMinutes)), available = !busy.Contains(s) }) });
        })
        .WithSummary("Szabad szervizidőpontok")
        .WithDescription("A `from`–`to` napok szervizidőpontjai (legfeljebb 31 nap, mától a foglalási időszak végéig); " +
            "az `available` mező jelzi, szabad-e. A válasz az időzónát és az időpontok hosszát is megadja. Hibás időszakra 400.");
        group.MapGet("/", async (ClaimsPrincipal user, BikeDbContext db, CancellationToken ct) =>
        {
            if (UserId(user) is not int id) return Results.Unauthorized();
            var items = await db.ServiceAppointments.AsNoTracking().Where(a => a.UserId == id)
                .OrderBy(a => a.AppointmentDate).ToListAsync(ct);
            return Results.Ok(items.Select(Response));
        })
        .WithSummary("Saját szervizfoglalásaim")
        .WithDescription("A bejelentkezett felhasználó szervizfoglalásai időrendben, státusszal (Pending, Confirmed, Completed, Cancelled).");
        group.MapPost("/", (ServiceAppointmentRequest request, ClaimsPrincipal user, BikeDbContext db,
            IOptions<ServiceSchedule> schedule, TimeProvider clock, CancellationToken ct) =>
            Book(null, request, user, db, schedule.Value, clock, ct))
        .WithSummary("Szervizidőpont foglalása")
        .WithDescription("Új szervizfoglalás Pending státusszal. Kötelező a kerékpár és a szerviz típusa (legfeljebb 100 karakter), " +
            "a leírás legfeljebb 1000 karakter. Csak jövőbeli, nyitvatartáson belüli időpont foglalható (különben 400), " +
            "foglalt időpontra 409.");
        group.MapPut("/{id:int}", (int id, ServiceAppointmentRequest request, ClaimsPrincipal user,
            BikeDbContext db, IOptions<ServiceSchedule> schedule, TimeProvider clock, CancellationToken ct) =>
            Book(id, request, user, db, schedule.Value, clock, ct))
        .WithSummary("Szervizfoglalás módosítása")
        .WithDescription("Saját, még nem esedékes Pending vagy Confirmed foglalás időpontját és adatait módosítja; " +
            "a státusz visszaáll Pending-re. Ugyanaz az ellenőrzés, mint foglaláskor. Nem módosítható foglalásra vagy " +
            "foglalt időpontra 409, nem létezőre 404.");
        group.MapDelete("/{id:int}", async (int id, ClaimsPrincipal user, BikeDbContext db,
            TimeProvider clock, CancellationToken ct) =>
        {
            if (UserId(user) is not int userId) return Results.Unauthorized();
            var count = await db.ServiceAppointments.Where(a => a.Id == id && a.UserId == userId &&
                (a.Status == "Pending" || a.Status == "Confirmed") && a.AppointmentDate > clock.GetUtcNow().UtcDateTime)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, "Cancelled"), ct);
            if (count > 0) return Results.NoContent();
            return await db.ServiceAppointments.AnyAsync(a => a.Id == id && a.UserId == userId, ct)
                ? Results.Conflict(new { message = "Ez a foglalás már nem mondható le." }) : Results.NotFound();
        })
        .WithSummary("Szervizfoglalás lemondása")
        .WithDescription("Cancelled státuszra állítja a foglalást, nem törli. Csak saját, még nem esedékes Pending vagy " +
            "Confirmed foglalás mondható le, különben 409. Nem létezőre 404.");
        var admin = group.MapGroup("/admin").RequireAuthorization(AuthConstants.AdminPolicy);
        admin.MapGet("/", async (DateOnly from, DateOnly to, BikeDbContext db, IOptions<ServiceSchedule> options, CancellationToken ct) =>
        {
            if (to < from || to == DateOnly.MaxValue || to.DayNumber - from.DayNumber > 366) return Error("Érvénytelen dátumtartomány.");
            var zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
            var start = TimeZoneInfo.ConvertTimeToUtc(from.ToDateTime(TimeOnly.MinValue), zone);
            var end = TimeZoneInfo.ConvertTimeToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
            var items = await db.ServiceAppointments.AsNoTracking().Where(a => a.AppointmentDate >= start && a.AppointmentDate < end)
                .OrderBy(a => a.AppointmentDate).ToListAsync(ct);
            return Results.Ok(items.Select(a => new { a.UserId, appointment = Response(a) }));
        })
        .WithSummary("Szervizfoglalások (admin)")
        .WithDescription("Minden felhasználó szervizfoglalása a `from`–`to` napokon (a szerviz időzónája szerint), " +
            "a foglaló azonosítójával. Legfeljebb kb. egy év kérhető le, hibás tartományra 400. Csak admin.");
        admin.MapPatch("/{id:int}/status", async (int id, ServiceStatusRequest request, BikeDbContext db, CancellationToken ct) =>
        {
            string[] previous = request.Status switch {
                "Confirmed" => ["Pending"], "Completed" => ["Confirmed"],
                "Cancelled" => ["Pending", "Confirmed"], _ => [] };
            if (previous.Length == 0) return Error("Engedélyezett státusz: Confirmed, Completed, Cancelled.");
            var count = await db.ServiceAppointments.Where(a => a.Id == id && previous.Contains(a.Status))
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, request.Status), ct);
            if (count > 0) return Results.NoContent();
            return await db.ServiceAppointments.AnyAsync(a => a.Id == id, ct)
                ? Results.Conflict(new { message = "Nem engedélyezett státuszváltás." }) : Results.NotFound();
        })
        .WithSummary("Szervizstátusz módosítása")
        .WithDescription("Engedélyezett váltások: Pending → Confirmed, Confirmed → Completed, Pending vagy Confirmed → Cancelled. " +
            "Más célstátuszra 400, nem engedélyezett váltásra 409, nem létező foglalásra 404. Csak admin.");
    }

    private static async Task<IResult> Book(int? id, ServiceAppointmentRequest request, ClaimsPrincipal user,
        BikeDbContext db, ServiceSchedule schedule, TimeProvider clock, CancellationToken ct)
    {
        if (UserId(user) is not int userId) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.BikeType) || request.BikeType.Trim().Length > 100 ||
            string.IsNullOrWhiteSpace(request.ServiceType) || request.ServiceType.Trim().Length > 100 ||
            request.ProblemDescription?.Length > 1000)
            return Error("A kerékpár és a szerviz típusa kötelező (legfeljebb 100 karakter), a leírás legfeljebb 1000 karakter.");
        var now = clock.GetUtcNow().UtcDateTime;
        var date = request.AppointmentDate.UtcDateTime;
        if (!schedule.CanBook(date, now)) return Error("Válassz egy jövőbeli, nyitvatartáson belüli szabad időpontot.");
        // SQLite BEGIN IMMEDIATE: az ellenőrzés és az írás több szerverpéldány esetén is sorosított.
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: false);
        await db.Database.UseTransactionAsync(transaction, ct);
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct)) return Results.Unauthorized();
        ServiceAppointment? item = null;
        if (id.HasValue)
        {
            item = await db.ServiceAppointments.SingleOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
            if (item == null) return Results.NotFound();
            if (item.Status is not ("Pending" or "Confirmed") || item.AppointmentDate <= now)
                return Results.Conflict(new { message = "Ez a foglalás már nem módosítható." });
        }
        if (await db.ServiceAppointments.AnyAsync(a => a.AppointmentDate == date && a.Status != "Cancelled" &&
            (!id.HasValue || a.Id != id.Value), ct))
            return Results.Conflict(new { message = "Ez az időpont már foglalt. Válassz másikat." });
        if (item == null) { item = new ServiceAppointment { UserId = userId, CreatedAt = now }; db.ServiceAppointments.Add(item); }
        item.AppointmentDate = date;
        item.BikeType = request.BikeType.Trim();
        item.ServiceType = request.ServiceType.Trim();
        item.ProblemDescription = request.ProblemDescription?.Trim();
        item.Status = "Pending";
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return id.HasValue ? Results.Ok(Response(item)) : Results.Created("/api/service-appointments", Response(item));
    }
}
