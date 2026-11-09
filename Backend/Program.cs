using Backend.Auth;
using Backend.Data;
using Backend.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Adatbázis (SQLite)
builder.Services.AddDbContext<BikeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=bikerental.db"));

// JWT hitelesítés + jogosultságkezelés
builder.Services.AddJwtAuth(builder.Configuration);

builder.Services.AddOptions<Backend.Services.ServiceSchedule>()
    .Bind(builder.Configuration.GetSection("ServiceSchedule"))
    .Validate(s => s.OpeningHour >= 0 && s.ClosingHour <= 23 && s.ClosingHour > s.OpeningHour &&
        s.SlotMinutes > 0 && s.SlotMinutes <= (s.ClosingHour - s.OpeningHour) * 60 &&
        s.BookingHorizonDays is > 0 and <= 365, "Érvénytelen szerviz nyitvatartás.")
    .Validate(s => TimeZoneInfo.TryFindSystemTimeZoneById(s.TimeZoneId, out _), "Érvénytelen időzóna.")
    .ValidateOnStart();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT token a /api/auth/login válaszából"
    });
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // A fejlesztői adatbázis sémája induláskor követi a letöltött kódot.
    // A szükséges migráció nélkül az új, foglalás nélküli értékelés 500-at adna.
    await using var scope = app.Services.CreateAsyncScope();
    await DevelopmentDatabase.InitializeAsync(scope.ServiceProvider.GetRequiredService<BikeDbContext>());

    app.UseSwagger();
    app.UseSwaggerUI();
}

// A Vite fejlesztői proxy a launchSettings.json szerinti HTTP-portot használja.
// Fejlesztésben a HTTPS-re irányítás a böngészőt a backend külön HTTPS-portjára
// küldené át, megkerülve a proxyt és a helyi fejlesztői tanúsítványt.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Modulok regisztrációja
app.MapAuthEndpoints();
app.MapBikeEndpoints();
app.MapRentalEndpoints();
app.MapServiceAppointmentEndpoints();
app.MapReviewEndpoints();

app.Run();

public partial class Program { }
