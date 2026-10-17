using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth").WithTags("Auth");

        // Regisztráció
        group.MapPost("/register", async (UserRegisterDto dto, BikeDbContext db) =>
        {
            if (await db.Users.AnyAsync(u => u.Email == dto.Email))
                return Results.BadRequest(new { message = "Ez az email cím már regisztrálva van." });

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Customer"
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Sikeres regisztráció!" });
        });

        // Bejelentkezés
        group.MapPost("/login", async (UserLoginDto dto, BikeDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Results.BadRequest(new { message = "Hibás email vagy jelszó." });

            return Results.Ok(new { message = "Sikeres bejelentkezés!", username = user.Username, role = user.Role });
        });
    }
}