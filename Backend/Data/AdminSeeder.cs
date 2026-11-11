using Backend.Auth;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class AdminSeeder
{
    // Regisztrációval csak Customer jön létre, ezért az első admin fiók a konfigurációból
    // (Admin:Email, Admin:Password, Admin:Username) készül. Meglévő fióknál csak a szerepkör változik.
    public static async Task SeedAsync(BikeDbContext db, IConfiguration config, CancellationToken ct = default)
    {
        var email = config["Admin:Email"];
        var password = config["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null)
        {
            db.Users.Add(new User
            {
                Username = config["Admin:Username"] ?? "Admin",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = AuthConstants.AdminRole
            });
        }
        else if (user.Role != AuthConstants.AdminRole)
        {
            user.Role = AuthConstants.AdminRole;
        }
        else
        {
            return;
        }

        await db.SaveChangesAsync(ct);
    }
}
