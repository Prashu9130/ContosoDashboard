using Microsoft.Data.Sqlite;

namespace ContosoDashboard.Tests.TestSupport;

public sealed class DocumentTestFixture : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    public ApplicationDbContext Db { get; private set; } = null!;
    public FakeFileStorageService Storage { get; } = new();
    public FakeMalwareScanner Scanner { get; } = new();
    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "contoso-document-tests", Guid.NewGuid().ToString("N"));
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(StorageRoot);
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        Db = new ApplicationDbContext(options);
        await Db.Database.EnsureCreatedAsync();
    }
    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
        await connection.DisposeAsync();
        if (Directory.Exists(StorageRoot)) Directory.Delete(StorageRoot, true);
    }
}