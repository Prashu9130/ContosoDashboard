# Phase 0 Research: Document Upload and Management

## Decision: Layered document workflow with storage, scanning, and authorization abstractions

**Rationale:** The existing application separates Blazor pages from EF-backed services, and the feature must preserve that boundary while allowing local training implementations to be replaced later. The plan will introduce a document service as the business orchestrator, an `IFileStorageService` for protected local filesystem operations, and an `IMalwareScanner` for the offline security gate. UI pages will submit validated metadata and report per-file results; they will not make authorization or storage decisions.

**Alternatives considered:** Direct file operations in Razor components were rejected because they duplicate security rules and make cloud migration and integration testing harder. A cloud SDK was rejected because the training application must run without cloud services.

## Decision: Scan before publication and treat scanner failure as rejection

**Rationale:** Each selected file is validated for size, extension, content type, metadata, and association before temporary handling. The scanner receives the temporary stream or storage reference and returns a clean, infected, or unavailable/incomplete outcome. Only a clean result may proceed to the durable metadata commit. Infected and unavailable/incomplete results return actionable per-file errors; temporary bytes are deleted and no usable `Document` row is committed. This directly implements the clarified FR-005 behavior.

**Alternatives considered:** Treating scanner unavailability as a warning was rejected because it would make an unverified file available. Persisting a pending document row was rejected because the requirement says no usable document record is retained when scanning cannot complete; transient state belongs in the workflow, not the user-visible document table.

## Decision: Local storage uses generated relative keys outside `wwwroot`

**Rationale:** The local implementation stores files below a configured application-data root, with a generated GUID filename and normalized relative key such as `{userId}/{project-or-personal}/{guid}.{allowlisted-extension}`. User filenames are display metadata only. The storage abstraction exposes save, read, delete, and existence/cleanup operations using opaque relative keys. Endpoints or service methods authorize the document before requesting bytes, so no static-file route exposes content.

**Alternatives considered:** Storing the original name or absolute path was rejected due to path traversal, collisions, portability, and leakage of machine-specific paths. Storing bytes in SQLite was rejected because it conflicts with the stakeholder local-file requirement and unnecessarily enlarges the database.

## Decision: Consistency uses compensating cleanup around the file/metadata boundary

**Rationale:** Local filesystem and SQLite cannot share one atomic transaction. The upload sequence is: validate and authorize; generate key; write temporary/protected bytes; scan; add metadata; save the database; then finalize the storage key. Any failure removes the temporary/final file and leaves no usable row. Replacement writes a new generated key and validates/commits it before deleting the old file; deletion removes the database row and then the file with an explicit failure/repair result, preserving a documented invariant that no authorized record points at missing content. Audit and notifications are emitted only after the document operation succeeds.

**Alternatives considered:** Saving the database row first was rejected because it creates orphaned metadata when storage fails. A distributed transaction was rejected as disproportionate and unavailable for the local filesystem/SQLite training stack.

## Decision: Explicit relational entities for sharing, task links, and activity

**Rationale:** `Document` keeps required metadata and foreign keys to uploader, optional project, and optional task. `DocumentShare` represents a user or team recipient with a constrained target shape, actor, and timestamp. `DocumentActivity` records upload, replacement, download, preview, deletion, and share actions with actor, document reference, timestamp, and useful request metadata. Task/project links are explicit foreign keys so project authorization can be expressed in database queries and task uploads can verify that the task's project matches the requested association.

**Alternatives considered:** A serialized JSON permissions field was rejected because it makes authorization queries, uniqueness, and auditability weak. A generic many-to-many join without recipient type was rejected because the requirement distinguishes individual users and teams/departments.

## Decision: Authorization is applied before projection and byte access

**Rationale:** Query methods will build an authorized document predicate from ownership, project manager/member access, explicit user shares, team/department shares, and administrator authority. The same predicate is reused for list, search, preview, download, update, replace, delete, and share operations. Search filters and sorting are applied after authorization but before materialization; projections include only display metadata. Preview is limited to PDF/JPEG/PNG and uses the stored MIME type only after extension/content validation.

**Alternatives considered:** Filtering unauthorized results in the UI was rejected because it leaks data and is not sufficient for forged identifiers or direct requests. A single broad administrator bypass in UI code was rejected; administrator access must be enforced in the service boundary and audit actions.

## Decision: Performance is verified against the documented training scale

**Rationale:** Add indexes for uploader/date, project/date, category/date, file type, and share recipient/document relationships. Search uses the authorized query and case-insensitive SQLite-compatible predicates across title, description, tags, uploader display name, and project name. The quickstart will validate list/search at up to 500 documents and record timing against the 2-second targets, plus upload, preview, and authorization scenarios.

**Alternatives considered:** Full-text search infrastructure was rejected for this release because it adds offline deployment complexity and is unnecessary for the stated 500-document scale.

## Resolved technical unknowns

- **Language/version:** C# on .NET 10, based on `ContosoDashboard.csproj`.
- **Web framework:** ASP.NET Core Blazor Server with the existing Razor pages/components and service registration.
- **Persistence:** Existing EF Core SQLite database and integer key conventions.
- **Storage:** Local filesystem outside `wwwroot`, behind `IFileStorageService`.
- **Security gate:** `IMalwareScanner`; unavailable or incomplete is a hard rejection with cleanup.
- **Testing:** Existing build plus focused service/integration verification against SQLite and temporary storage; browser acceptance checks for the Blazor workflows.
- **External contracts:** Protected HTTP endpoints for preview/download and service contracts for upload, query, management, sharing, and audit reporting; documented in `contracts/document-management.md`.
