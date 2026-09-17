using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext db;
    private readonly IFileStorageService storage;
    private readonly IMalwareScanner scanner;
    private static readonly IReadOnlyDictionary<string, string> TypeByExtension = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf", [".doc"] = "application/msword", [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel", [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation", [".txt"] = "text/plain", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png"
    };

    public DocumentService(ApplicationDbContext db, IFileStorageService storage, IMalwareScanner scanner) { this.db = db; this.storage = storage; this.scanner = scanner; }

    public async Task<IReadOnlyList<UploadResult>> UploadAsync(int userId, DocumentMetadata metadata, IReadOnlyList<UploadFileInput> files, IProgress<UploadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var results = new List<UploadResult>();
        foreach (var file in files)
        {
            results.Add(await UploadOneAsync(userId, metadata, file, progress, cancellationToken));
        }
        return results;
    }

    private async Task<UploadResult> UploadOneAsync(int userId, DocumentMetadata metadata, UploadFileInput file, IProgress<UploadProgress>? progress, CancellationToken cancellationToken)
    {
        var metadataError = ValidateMetadata(metadata);
        if (metadataError is not null) return UploadResult.Rejected(file.Name, DocumentRejectionCode.MetadataInvalid, metadataError);
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!TypeByExtension.TryGetValue(extension, out var expectedType) || !string.Equals(expectedType, file.ContentType, StringComparison.OrdinalIgnoreCase)) return UploadResult.Rejected(file.Name, DocumentRejectionCode.UnsupportedType, "The file type is not supported.");
        if (file.Length <= 0 || file.Length > Document.MaxFileSize) return UploadResult.Rejected(file.Name, DocumentRejectionCode.FileTooLarge, "Each file must be between 1 byte and 25 MiB.");
        if (!await IsAssociationAuthorizedAsync(userId, metadata, cancellationToken)) return UploadResult.Rejected(file.Name, DocumentRejectionCode.AssociationUnauthorized, "You are not authorized for the selected project or task.");

        string? storageKey = null;
        try
        {
            await using var source = await file.OpenReadAsync(cancellationToken);
            if (source is null) return UploadResult.Rejected(file.Name, DocumentRejectionCode.FileUnavailable, "The selected file is no longer available.");
            await using var content = new MemoryStream();
            var buffer = new byte[64 * 1024];
            int read;
            long processed = 0;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                processed += read;
                if (processed > Document.MaxFileSize) return UploadResult.Rejected(file.Name, DocumentRejectionCode.FileTooLarge, "Each file must be at most 25 MiB.");
                await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                progress?.Report(new UploadProgress(file.Name, processed, file.Length));
            }
            if (processed == 0) return UploadResult.Rejected(file.Name, DocumentRejectionCode.FileUnavailable, "The selected file is empty or unavailable.");
            content.Position = 0;
            var scan = await scanner.ScanAsync(content, new ScanContext(file.Name, file.ContentType), cancellationToken);
            if (scan.Status != MalwareScanStatus.Clean)
            {
                var code = scan.Status switch { MalwareScanStatus.Infected => DocumentRejectionCode.ScanInfected, MalwareScanStatus.Incomplete => DocumentRejectionCode.ScanIncomplete, _ => DocumentRejectionCode.ScanUnavailable };
                return UploadResult.Rejected(file.Name, code, scan.Message);
            }
            content.Position = 0;
            var write = await storage.SaveAsync(content, new StorageDescriptor(extension), cancellationToken);
            if (!write.Succeeded || string.IsNullOrWhiteSpace(write.StorageKey)) return UploadResult.Rejected(file.Name, DocumentRejectionCode.StorageFailed, write.ErrorMessage ?? "The file could not be stored.");
            storageKey = write.StorageKey;
            var now = DateTime.UtcNow;
            var document = new Document { Title = metadata.Title.Trim(), Description = NormalizeOptional(metadata.Description), Category = metadata.Category, Tags = NormalizeOptional(metadata.Tags), OriginalFileName = Path.GetFileName(file.Name), StorageKey = storageKey, FileType = file.ContentType, FileExtension = extension, FileSize = processed, UploaderId = userId, ProjectId = metadata.ProjectId, TaskId = metadata.TaskId, UploadedDate = now, UpdatedDate = now };
            db.Documents.Add(document);
            db.DocumentActivities.Add(new DocumentActivity { Document = document, ActorUserId = userId, ActivityType = "Upload", OccurredDate = now, FileType = file.ContentType, FileSize = processed });
            await db.SaveChangesAsync(cancellationToken);
            return UploadResult.Success(file.Name, document.DocumentId);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateException) { if (storageKey is not null) await storage.DeleteAsync(storageKey, cancellationToken); return UploadResult.Rejected(file.Name, DocumentRejectionCode.PersistenceFailed, "The document could not be saved; no file was retained."); }
        catch (IOException) { if (storageKey is not null) await storage.DeleteAsync(storageKey, cancellationToken); return UploadResult.Rejected(file.Name, DocumentRejectionCode.FileUnavailable, "The selected file could not be read."); }
        catch (UnauthorizedAccessException) { if (storageKey is not null) await storage.DeleteAsync(storageKey, cancellationToken); return UploadResult.Rejected(file.Name, DocumentRejectionCode.StorageFailed, "The protected storage location is unavailable."); }
        catch (Exception) { if (storageKey is not null) await storage.DeleteAsync(storageKey, cancellationToken); return UploadResult.Rejected(file.Name, DocumentRejectionCode.PersistenceFailed, "The document could not be completed; no file was retained."); }
    }

    public async Task<IReadOnlyList<DocumentSummary>> SearchAsync(int userId, string? query = null, DocumentFilters? filters = null, DocumentSort sort = DocumentSort.UploadedDate, CancellationToken cancellationToken = default)
    {
        return await ProjectSummaries(AccessibleDocuments(userId).OrderByDescending(d => d.UploadedDate), cancellationToken);
    }
    public async Task<DocumentDetails?> GetByIdAsync(int userId, int documentId, CancellationToken cancellationToken = default)
    {
        if (documentId <= 0) return null;
        return null;
    }
    public Task<IReadOnlyList<DocumentSummary>> GetProjectDocumentsAsync(int userId, int projectId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DocumentSummary>>([]);
    public Task<IReadOnlyList<DocumentSummary>> GetSharedWithMeAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DocumentSummary>>([]);
    public Task<IReadOnlyList<DocumentSummary>> GetRecentAsync(int userId, int count = 5, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DocumentSummary>>([]);
    public Task<int> GetCountAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(0);
    public Task<OperationResult> UpdateMetadataAsync(int userId, int documentId, DocumentMetadata metadata, CancellationToken cancellationToken = default) => Task.FromResult(OperationResult.Failure("Metadata management is outside the MVP upload scope."));
    public Task<OperationResult> ReplaceFileAsync(int userId, int documentId, UploadFileInput file, CancellationToken cancellationToken = default) => Task.FromResult(OperationResult.Failure("File replacement is outside the MVP upload scope."));
    public Task<OperationResult> DeleteAsync(int userId, int documentId, CancellationToken cancellationToken = default) => Task.FromResult(OperationResult.Failure("Document deletion is outside the MVP upload scope."));
    public Task<OperationResult> ShareAsync(int userId, int documentId, DocumentRecipient recipient, CancellationToken cancellationToken = default) => Task.FromResult(OperationResult.Failure("Document sharing is outside the MVP upload scope."));

    private IQueryable<Document> AccessibleDocuments(int userId) => db.Documents.AsNoTracking().Include(d => d.Uploader).Include(d => d.Project).Include(d => d.Shares).ThenInclude(s => s.User).Where(d => d.UploaderId == userId || db.Users.Any(u => u.UserId == userId && u.Role == UserRole.Administrator) || (d.ProjectId.HasValue && db.Projects.Any(p => p.ProjectId == d.ProjectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId)))) || d.Shares.Any(s => s.UserId == userId || (s.TeamName != null && db.Users.Any(u => u.UserId == userId && u.Department == s.TeamName))));
    private async Task<bool> IsAssociationAuthorizedAsync(int userId, DocumentMetadata metadata, CancellationToken cancellationToken)
    {
        if (!metadata.ProjectId.HasValue && !metadata.TaskId.HasValue) return true;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user?.Role == UserRole.Administrator) return true;
        if (!metadata.ProjectId.HasValue) return false;
        var project = await db.Projects.Include(p => p.ProjectMembers).SingleOrDefaultAsync(p => p.ProjectId == metadata.ProjectId, cancellationToken);
        if (project is null || (project.ProjectManagerId != userId && !project.ProjectMembers.Any(m => m.UserId == userId))) return false;
        return !metadata.TaskId.HasValue || await db.Tasks.AnyAsync(t => t.TaskId == metadata.TaskId && t.ProjectId == metadata.ProjectId, cancellationToken);
    }
    private async Task<IReadOnlyList<DocumentSummary>> ProjectSummaries(IQueryable<Document> query, CancellationToken cancellationToken) => await query.Select(d => new DocumentSummary(d.DocumentId, d.Title, d.Category, d.UploadedDate, d.Uploader.DisplayName, d.FileSize, d.FileType, d.OriginalFileName, d.Project == null ? null : d.Project.Name, d.Tags)).ToListAsync(cancellationToken);
    private static string? ValidateMetadata(DocumentMetadata metadata) => string.IsNullOrWhiteSpace(metadata.Title) || metadata.Title.Trim().Length > 255 || string.IsNullOrWhiteSpace(metadata.Category) || !Document.AllowedCategories.Contains(metadata.Category, StringComparer.Ordinal) || metadata.Description?.Length > 2000 || metadata.Tags?.Length > 1000 ? "Title, category, description, or tags are invalid." : null;
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}