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

app.Run();

public partial class Program { }
