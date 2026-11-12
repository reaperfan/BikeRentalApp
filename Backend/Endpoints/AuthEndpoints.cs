using System.Security.Claims;
using Backend.Auth;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Backend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth").WithTags("Auth");
        var anonymous = group.MapGroup("").AllowAnonymous();

        // Regisztráció
        anonymous.MapPost("/register", async (UserRegisterDto dto, BikeDbContext db) =>
        {
            if (await db.Users.AnyAsync(u => u.Email == dto.Email))
                return Results.BadRequest(new { message = "Ez az email cím már regisztrálva van." });

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = AuthConstants.CustomerRole
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Sikeres regisztráció!" });
        })
        .WithSummary("Regisztráció")
        .WithDescription("Új felhasználói fiókot hoz létre Customer szerepkörrel. A jelszó BCrypt hash-ként tárolódik. " +
            "Már regisztrált e-mail címre 400. Bejelentkezés nem kell.");

        // Bejelentkezés
        anonymous.MapPost("/login", async (UserLoginDto dto, BikeDbContext db, ITokenService tokens) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Results.Json(new { message = "Hibás email vagy jelszó." }, statusCode: StatusCodes.Status401Unauthorized);

            var token = tokens.CreateToken(user);
            return Results.Ok(new LoginResponseDto(token.AccessToken, token.ExpiresAt, user.Username, user.Role));
        })
        .WithSummary("Bejelentkezés")
        .WithDescription("E-mail és jelszó alapján JWT tokent ad (60 percig érvényes) a felhasználónévvel és a szerepkörrel együtt. " +
            "A tokent a további kérésekben az `Authorization: Bearer <token>` fejlécben kell küldeni (Swaggerben: Authorize gomb). " +
            "Hibás e-mailre vagy jelszóra 401.");

        // Bejelentkezett felhasználó adatai (érvényes token kell)
        group.MapGet("/me", async (ClaimsPrincipal principal, BikeDbContext db) =>
        {
            if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
                return Results.Unauthorized();

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Results.Unauthorized();

            return Results.Ok(new UserInfoDto(user.Id, user.Username, user.Email, user.Role, user.CreatedAt));
        })
        .WithSummary("Saját adatok")
        .WithDescription("A tokenhez tartozó felhasználó adatai: azonosító, név, e-mail, szerepkör, regisztráció ideje. " +
            "A frontend ezzel ellenőrzi induláskor, hogy a tárolt token még érvényes-e. Token nélkül vagy lejárt tokennel 401.");
    }
}