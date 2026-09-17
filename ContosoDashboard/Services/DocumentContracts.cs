using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public enum DocumentRejectionCode { MetadataInvalid, UnsupportedType, FileTooLarge, AssociationUnauthorized, FileUnavailable, ScanInfected, ScanUnavailable, ScanIncomplete, StorageFailed, PersistenceFailed }
public enum MalwareScanStatus { Clean, Infected, Unavailable, Incomplete }
public enum DocumentSort { Title, UploadedDate, Category, Size }

public sealed class DocumentMetadata
{
    public DocumentMetadata(string Title = "", string Category = "", string? Description = null, string? Tags = null, int? ProjectId = null, int? TaskId = null) { this.Title = Title; this.Category = Category; this.Description = Description; this.Tags = Tags; this.ProjectId = ProjectId; this.TaskId = TaskId; }
    public string Title { get; set; }
    public string Category { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
}
public sealed record UploadFileInput(string Name, string ContentType, long Length, Func<CancellationToken, Task<Stream>> OpenReadAsync);
public sealed record UploadProgress(string FileName, long BytesProcessed, long TotalBytes);
public sealed record UploadResult(string FileName, bool Succeeded, int? DocumentId, DocumentRejectionCode? RejectionCode, string Message)
{
    public static UploadResult Success(string fileName, int documentId) => new(fileName, true, documentId, null, "Upload completed.");
    public static UploadResult Rejected(string fileName, DocumentRejectionCode code, string message) => new(fileName, false, null, code, message);
}
public sealed record DocumentSummary(int DocumentId, string Title, string Category, DateTime UploadedDate, string UploaderName, long FileSize, string FileType, string OriginalFileName, string? ProjectName, string? Tags);
public sealed record DocumentDetails(Document Document);
public sealed record DocumentFilters(string? Category = null, int? ProjectId = null, DateTime? FromUtc = null, DateTime? ToUtc = null);
public sealed record DocumentRecipient(int? UserId = null, string? TeamName = null);
public sealed record OperationResult(bool Succeeded, string Message, DocumentRejectionCode? ErrorCode = null)
{
    public static OperationResult Success(string message = "Operation completed.") => new(true, message);
    public static OperationResult Failure(string message, DocumentRejectionCode? code = null) => new(false, message, code);
}
public sealed record StorageDescriptor(string Extension);
public sealed record StorageWriteResult(bool Succeeded, string? StorageKey, string? ErrorMessage = null);
public sealed record StorageDeleteResult(bool Succeeded, bool Existed, string? ErrorMessage = null);
public sealed record ScanContext(string FileName, string ContentType);
public sealed record MalwareScanResult(MalwareScanStatus Status, string Message);
public sealed record DocumentActivityReport(int Uploads, int Downloads, int Previews, int Deletes, int Shares, IReadOnlyDictionary<string, int> DocumentTypes, IReadOnlyDictionary<string, int> ActiveUploaders, IReadOnlyDictionary<string, int> AccessPatterns);

public interface IDocumentService
{
    Task<IReadOnlyList<UploadResult>> UploadAsync(int userId, DocumentMetadata metadata, IReadOnlyList<UploadFileInput> files, IProgress<UploadProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentSummary>> SearchAsync(int userId, string? query = null, DocumentFilters? filters = null, DocumentSort sort = DocumentSort.UploadedDate, CancellationToken cancellationToken = default);
    Task<DocumentDetails?> GetByIdAsync(int userId, int documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentSummary>> GetProjectDocumentsAsync(int userId, int projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentSummary>> GetSharedWithMeAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentSummary>> GetRecentAsync(int userId, int count = 5, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateMetadataAsync(int userId, int documentId, DocumentMetadata metadata, CancellationToken cancellationToken = default);
    Task<OperationResult> ReplaceFileAsync(int userId, int documentId, UploadFileInput file, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(int userId, int documentId, CancellationToken cancellationToken = default);
    Task<OperationResult> ShareAsync(int userId, int documentId, DocumentRecipient recipient, CancellationToken cancellationToken = default);
}
public interface IFileStorageService
{
    Task<StorageWriteResult> SaveAsync(Stream content, StorageDescriptor descriptor, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<StorageDeleteResult> DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}
public interface IMalwareScanner { Task<MalwareScanResult> ScanAsync(Stream content, ScanContext context, CancellationToken cancellationToken = default); }
public interface IDocumentReportService { Task<DocumentActivityReport> GetActivityReportAsync(int adminUserId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default); }