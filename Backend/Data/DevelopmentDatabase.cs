using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class DevelopmentDatabase
{
    public static async Task InitializeAsync(BikeDbContext db, CancellationToken ct = default)
    {
        // Két régi migráció azonosítójában láthatatlan szóelválasztó szerepelt.
        // A fájlok átnevezése után is ugyanazok a már létrehozott táblák érvényesek.
        var applied = (await db.Database.GetAppliedMigrationsAsync(ct)).ToHashSet();
        foreach (var (oldId, currentId) in new[]
        {
            ("20261101183220_\u2060ServiceAppointments", "20261101183220_ServiceAppointments"),
            ("20261101183543_\u2060Reviews", "20261101183543_Reviews")
        })
        {
            if (applied.Contains(oldId) && !applied.Contains(currentId))
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE \"__EFMigrationsHistory\" SET \"MigrationId\" = {currentId} WHERE \"MigrationId\" = {oldId}", ct);
        }
        await db.Database.MigrateAsync(ct);
    }
}
