# Data Model: Document Upload and Management

## Document

The durable record for an accepted, clean, authorized work file.

| Field | Type/constraint | Notes |
|---|---|---|
| `DocumentId` | `int`, required, key | Preserves existing key convention. |
| `Title` | `string`, required, max 255 | Trimmed; cannot be blank. |
| `Description` | `string?`, max 2000 | Optional. |
| `Category` | `string`, required, max 100 | Text value; allowlist: Project Documents, Team Resources, Personal Files, Reports, Presentations, Other. |
| `Tags` | `string?`, max 1000 | Normalized delimited tags; each tag is non-empty and bounded. |
| `OriginalFileName` | `string`, required, max 255 | Display-only metadata; never used as a storage path. |
| `StorageKey` | `string`, required, max 500, unique | Generated relative key under protected storage; no absolute paths. |
| `FileType` | `string`, required, max 255 | Validated MIME type. |
| `FileExtension` | `string`, required, max 20 | Allowlisted extension, normalized. |
| `FileSize` | `long`, required | Greater than zero and at most 25 MiB per file. |
| `UploaderId` | `int`, required, FK `User` | Immutable owner/audit actor for initial upload. |
| `ProjectId` | `int?`, FK `Project` | Must be null or an authorized project association. |
| `TaskId` | `int?`, FK `TaskItem` | If set, task must be accessible and its project must equal `ProjectId`. |
| `UploadedDate` | `DateTime`, required UTC | Set by the service. |
| `UpdatedDate` | `DateTime`, required UTC | Changes on metadata/file replacement. |

A document row is created only after successful validation, malware scan, protected storage write, and metadata persistence. A row with no valid storage key is never usable or exposed.

## DocumentShare

A permission grant from one document owner/authorized manager to a user or team/department.

- `DocumentShareId`: integer key.
- `DocumentId`: required FK; cascade/delete behavior must not leave an active share for a deleted document.
- `UserId`: nullable FK for an individual recipient.
- `TeamName`: nullable normalized team/department value for a group recipient.
- `SharedByUserId`: required FK to the actor.
- `SharedDate`: required UTC timestamp.

Exactly one of `UserId` and `TeamName` is populated. A uniqueness constraint prevents duplicate active grants for the same document and target. Sharing is authorized by the document owner, project manager for the document project, or administrator; the recipient is notified only after the share persists.

## DocumentActivity

Immutable audit record for document operations.

- `DocumentActivityId`: integer key.
- `DocumentId`: required FK for activity tied to a retained document; deletion activity may retain a nullable document reference if the document row is removed.
- `ActorUserId`: required FK to the authenticated actor.
- `ActivityType`: text value for Upload, Replace, Download, Preview, Delete, Share, or ShareRevoked.
- `OccurredDate`: required UTC timestamp.
- `FileType`, `FileSize`, `TargetUserId`, `TargetTeamName`, and `Details`: optional bounded context fields for reports and investigation.

Activities are written only for successful operations, except a rejected upload may be recorded as a security/validation event without creating a document record. Audit reads are administrator-only and date-filtered.

## Existing entity associations

- `User` has many uploaded documents, shares, and activities.
- `Project` has many documents; project manager and members authorize access.
- `TaskItem` has many documents; task visibility and the task's project govern attachment access.
- `Notification` remains the existing in-app notification entity and receives share and new-project-document notifications.

## State and consistency rules

1. **Selected -> Validating:** validate title/category, extension/MIME allowlist, size, tags, project/task association, and authorization.
2. **Validating -> Scanning:** copy the browser stream to a bounded temporary stream/storage object.
3. **Scanning -> Rejected:** infected, unavailable, incomplete, invalid, or failed storage/DB operation returns an actionable per-file error and removes temporary/final bytes; no usable document row remains.
4. **Scanning -> Published:** only a clean result may commit the document metadata and publish the storage key.
5. **Published -> Replaced:** validate and scan the new content, publish a new generated key, update metadata, then remove the old key.
6. **Published -> Deleted:** authorize and confirm, remove the document from user-visible queries, delete the stored file, and record the audit event. A failed cleanup must be surfaced for repair rather than silently reported as success.

## Indexes and query requirements

Add indexes for `(UploaderId, UploadedDate)`, `(ProjectId, UploadedDate)`, `(Category, UploadedDate)`, `FileType`, and document-share target columns. Query services must apply the authorization predicate before materialization and use `AsNoTracking` for list/search/report reads. The target data set is up to 500 documents per project and must meet the documented 2-second list/search goals.
