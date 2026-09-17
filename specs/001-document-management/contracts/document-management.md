# Document Management Contracts

These contracts describe the service and protected HTTP boundary for the Blazor/EF Core application. UI components call the service layer; they do not access the database or filesystem directly.

## Service contracts

### `IDocumentService`

- `UploadAsync(userId, metadata, files, progress, cancellationToken) -> IReadOnlyList<UploadResult>`
  - Validates each file independently and returns success or an actionable rejection for every selected file.
  - Rejects unsupported type, over-25-MiB input, invalid metadata/association, failed storage validation, infected content, or scanner unavailable/incomplete.
  - A rejected result has no usable document ID and no retained stored file.
- `SearchAsync(userId, query, filters, sort, cancellationToken) -> IReadOnlyList<DocumentSummary>`
  - Applies authorization before returning summaries; supports title, description, tags, uploader, and project search plus category/project/date filters and title/date/category/size sorting.
- `GetByIdAsync(userId, documentId, cancellationToken) -> DocumentDetails?`
  - Returns null/not-found semantics for nonexistent or unauthorized documents.
- `GetProjectDocumentsAsync(userId, projectId, cancellationToken) -> IReadOnlyList<DocumentSummary>`
- `GetSharedWithMeAsync(userId, cancellationToken) -> IReadOnlyList<DocumentSummary>`
- `GetRecentAsync(userId, count = 5, cancellationToken) -> IReadOnlyList<DocumentSummary>`
- `GetCountAsync(userId, cancellationToken) -> int`
- `UpdateMetadataAsync(userId, documentId, metadata, cancellationToken) -> OperationResult`
- `ReplaceFileAsync(userId, documentId, file, cancellationToken) -> OperationResult`
- `DeleteAsync(userId, documentId, cancellationToken) -> OperationResult`
- `ShareAsync(userId, documentId, recipient, cancellationToken) -> OperationResult`

Every mutating operation performs ownership/project-manager/administrator authorization in the service boundary and records the corresponding activity after success.

### `IFileStorageService`

- `SaveAsync(Stream content, StorageDescriptor descriptor, cancellationToken) -> StorageWriteResult`
- `OpenReadAsync(string storageKey, cancellationToken) -> Stream?`
- `DeleteAsync(string storageKey, cancellationToken) -> StorageDeleteResult`
- `ExistsAsync(string storageKey, cancellationToken) -> bool`

The contract accepts and returns opaque relative keys. Implementations must keep content outside `wwwroot`, generate or validate safe keys, prevent traversal, and support cleanup. A future provider can implement the same contract without changing document workflows.

### `IMalwareScanner`

- `ScanAsync(Stream content, ScanContext context, cancellationToken) -> MalwareScanResult`

Results are `Clean`, `Infected`, `Unavailable`, or `Incomplete`. Only `Clean` permits publication. `Unavailable` and `Incomplete` are user-visible actionable failures and require cleanup; they must never be treated as clean.

### `IDocumentReportService`

- `GetActivityReportAsync(adminUserId, fromUtc, toUtc, filters, cancellationToken) -> DocumentActivityReport`
- Report includes upload/download/delete/share counts and grouped document types, active uploaders, and access patterns. Administrator authorization is required.

## Protected HTTP endpoints

The application exposes preview/download through authorization-mediated endpoints, for example:

- `GET /documents/{documentId}/download`
- `GET /documents/{documentId}/preview`

The endpoint authenticates the current user, parses a positive integer identifier, calls the authorized document service, and returns `404` for nonexistent or unauthorized documents to avoid disclosure. It never accepts a storage path or filename from the caller. Download returns the stored MIME type and a safe display filename; preview is allowed only for PDF, JPEG, and PNG. Missing/deleted content returns an unavailable result and creates no fallback public URL.

## Upload result contract

Each submitted file returns:

- original display name
- success or rejection status
- document ID only on success
- reason code and actionable message on rejection
- progress is reported during stream copy/scan where the UI can display it

Required rejection reason codes include `MetadataInvalid`, `UnsupportedType`, `FileTooLarge`, `AssociationUnauthorized`, `FileUnavailable`, `ScanInfected`, `ScanUnavailable`, `ScanIncomplete`, `StorageFailed`, and `PersistenceFailed`.

## Authorization contract

An authenticated user may access a document when they are the uploader, an authorized member/manager of its project, an explicitly shared user, a matching shared team/department recipient, or an administrator. Management is narrower: owners may manage their documents, project managers may manage documents in their projects, and administrators may audit/manage under the documented administrator policy. All list, search, preview, download, update, replace, delete, and share calls use this boundary.
