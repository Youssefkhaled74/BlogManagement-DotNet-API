using System.ComponentModel.DataAnnotations;

namespace BlogManagement.Api.Uploads;

public sealed class ImageUploadRequest
{
    [Required] public IFormFile File { get; set; } = null!;
}

public sealed class ImageStorage(IWebHostEnvironment environment)
{
    public const int MaxBytes = 5 * 1024 * 1024;
    private readonly string _directory = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");

    public async Task<string> SaveAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > MaxBytes)
            throw new InvalidOperationException("Choose an image smaller than 5 MB.");
        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await input.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + count > MaxBytes) throw new InvalidOperationException("Image exceeds 5 MB.");
            await buffer.WriteAsync(chunk.AsMemory(0, count), ct);
        }
        var bytes = buffer.ToArray();
        var extension = DetectExtension(bytes);
        Directory.CreateDirectory(_directory);
        var name = $"{Guid.NewGuid():N}{extension}";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(_directory, name), bytes, ct);
        return $"/uploads/{name}";
    }

    public static string DetectExtension(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return ".png";
        if (bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) return ".jpg";
        if (bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP") return ".webp";
        throw new InvalidOperationException("Only PNG, JPEG, and WebP images are supported.");
    }

    public void Delete(string? url)
    {
        if (url is null || !url.StartsWith("/uploads/", StringComparison.Ordinal)) return;
        var name = url["/uploads/".Length..];
        if (name != Path.GetFileName(name) || name.Contains('\\') || name.Contains('/')) return;
        var path = Path.Combine(_directory, name);
        try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
