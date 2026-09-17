namespace ContosoDashboard.Tests.TestSupport;

public sealed class FakeFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, byte[]> files = new(StringComparer.Ordinal);
    public bool FailWrites { get; set; }
    public bool FailDeletes { get; set; }
    public IReadOnlyDictionary<string, byte[]> Files => files;
    public async Task<StorageWriteResult> SaveAsync(Stream content, StorageDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        if (FailWrites) return new(false, null, "Storage failure");
        var key = $"fake/{Guid.NewGuid():N}{descriptor.Extension}";
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        files[key] = memory.ToArray();
        return new(true, key);
    }
    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(files.TryGetValue(storageKey, out var bytes) ? new MemoryStream(bytes, writable: false) : null);
    public Task<StorageDeleteResult> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (FailDeletes) return Task.FromResult(new StorageDeleteResult(false, files.ContainsKey(storageKey), "Delete failure"));
        return Task.FromResult(new StorageDeleteResult(true, files.Remove(storageKey)));
    }
    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult(files.ContainsKey(storageKey));
}