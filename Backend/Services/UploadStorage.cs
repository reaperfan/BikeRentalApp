namespace Backend.Services;

// Feltöltött fájlok helye a lemezen; a /uploads útvonalon szolgáljuk ki őket.
public sealed class UploadStorage
{
    public const string RequestPath = "/uploads";
    public const string BikeFolder = "bikes";
    public const long MaxImageBytes = 5 * 1024 * 1024;

    public string RootPath { get; }

    public UploadStorage(string rootPath)
    {
        RootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(Path.Combine(RootPath, BikeFolder));
    }

    // A formátumot a fájl tartalmából állapítjuk meg, nem a kiterjesztésből.
    public static string? DetectImageExtension(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })) return ".jpg";
        if (header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }

    public async Task<string> SaveBikeImageAsync(Stream content, string extension, CancellationToken ct)
    {
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var file = File.Create(Path.Combine(RootPath, BikeFolder, fileName));
        await content.CopyToAsync(file, ct);
        return $"{RequestPath}/{BikeFolder}/{fileName}";
    }
}
