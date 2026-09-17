using System.Security.Cryptography;

namespace ContosoDashboard.Services;

public sealed class FileStorageService : IFileStorageService
{
    private readonly string root;
    public FileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration["DocumentStorage:Root"];
        root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured) ? Path.Combine(environment.ContentRootPath, "App_Data", "Documents") : (Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured)));
        Directory.CreateDirectory(root);
    }
    public async Task<StorageWriteResult> SaveAsync(Stream content, StorageDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        var extension = NormalizeExtension(descriptor.Extension);
        if (extension is null) return new(false, null, "The file extension is not allowed.");
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try { await using var output = File.Create(path); await content.CopyToAsync(output, cancellationToken); return new(true, key); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return new(false, null, ex.Message); }
    }
    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(TryResolve(storageKey, out var path) && File.Exists(path) ? File.OpenRead(path) : null);
    public async Task<StorageDeleteResult> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!TryResolve(storageKey, out var path)) return new(false, false, "Invalid storage key.");
        if (!File.Exists(path)) return new(true, false);
        try { File.Delete(path); await Task.CompletedTask; return new(true, true); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return new(false, true, ex.Message); }
    }
    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult(TryResolve(storageKey, out var path) && File.Exists(path));
    private string Resolve(string key) => Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
    private bool TryResolve(string key, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(key) || Path.IsPathRooted(key) || key.Contains("..", StringComparison.Ordinal)) return false;
        path = Path.GetFullPath(Resolve(key));
        return path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
    private static string? NormalizeExtension(string extension) => new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png" }.Contains(extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant()) ? (extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant()) : null;
}