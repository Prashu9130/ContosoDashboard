# Implementation Plan: Document Upload and Management

**Branch**: `001-document-management` | **Date**: 2026-09-18 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-document-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add secure, searchable document upload and management to the existing offline Blazor Server dashboard. The implementation will keep document business rules in an authorized service layer, persist integer-keyed EF Core metadata in SQLite, store generated-key files outside `wwwroot` behind `IFileStorageService`, and require a clean result from `IMalwareScanner` before publishing any document. Scanner unavailability or incomplete scanning is an actionable rejection with cleanup and no usable document record.

## Technical Context

**Language/Version**: C# / .NET 10 with nullable reference types enabled  
**Primary Dependencies**: ASP.NET Core Blazor Server, Entity Framework Core 10 SQLite, existing cookie/mock authentication and notification services  
**Storage**: Existing SQLite database plus local filesystem storage outside `wwwroot`, accessed through `IFileStorageService`  
**Testing**: Focused service/integration tests with SQLite and temporary storage, browser acceptance checks, and `dotnet build`  
**Target Platform**: Local/offline web application on the existing ContosoDashboard host  
**Project Type**: Single web application with Blazor UI, service layer, EF Core data layer, and local infrastructure  
**Performance Goals**: 95% of authorized list/search operations under 2 seconds for up to 500 documents; preview under 3 seconds; uploads up to 25 MiB under 30 seconds under typical conditions  
**Constraints**: No cloud services; 25 MiB per file; allowlisted types; protected storage; generated storage keys; clean malware scan required; no orphaned usable records/files  
**Scale/Scope**: Four user roles, projects/tasks already in the app, up to 500 documents per project, web-only initial release; version history and external providers remain out of scope

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Offline-First Training: PASS.** Uses SQLite and local filesystem implementations; scanner absence rejects uploads rather than implying production scanning. Cloud storage remains an interchangeable future provider.
- **II. Layered Architecture: PASS.** Blazor pages call document services; services own authorization and orchestration; EF and file storage are separated behind focused interfaces.
- **III. Security by Authorization: PASS.** All document reads, searches, bytes, and management operations use a service-level authorization predicate; paths, identifiers, extensions, associations, and scanner outcomes are validated.
- **IV. Verifiable Changes: PASS.** The quickstart defines focused persistence, cleanup, authorization, scanner, endpoint, and performance checks, followed by a project build.
- **V. Simple, Maintainable Evolution: PASS.** The design adds only entities and abstractions required by the feature and preserves integer IDs, text categories, existing notifications, projects, tasks, and authentication conventions.

**Phase 0 gate:** PASS. Research resolved all technical unknowns.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-management/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs              # document DbSets, relationships, indexes
├── Models/
│   ├── Document.cs
│   ├── DocumentShare.cs
│   └── DocumentActivity.cs
├── Services/
│   ├── DocumentService.cs                   # validation, authorization, orchestration
│   ├── FileStorageService.cs                # IFileStorageService + local implementation
│   ├── MalwareScanner.cs                    # IMalwareScanner and offline implementation/config
│   └── DocumentReportService.cs             # administrator reports
├── Pages/
│   ├── Documents.razor                      # upload, list, search/filter/sort
│   ├── SharedDocuments.razor
│   └── DocumentEndpoints.cs                 # authorized preview/download boundary
├── Shared/
│   └── MainLayout.razor                     # document navigation/dashboard surface as needed
└── wwwroot/                                 # no uploaded content

tests/                                       # focused project or test fixture location
├── Integration/
│   └── DocumentManagementTests.cs
└── Unit/
    ├── DocumentAuthorizationTests.cs
    └── UploadWorkflowTests.cs
```

**Structure Decision**: Extend the existing single `ContosoDashboard` web project using its established `Models`, `Data`, `Services`, `Pages`, and `Shared` directories. Add focused integration/unit test coverage in the repository's test location without introducing a second application or a cloud dependency. Protected file bytes remain outside `wwwroot` and are returned only through the authorized endpoint boundary.

## Complexity Tracking

No constitution violations. The storage and malware interfaces are required by FR-005, FR-007 through FR-009, and FR-022, and the explicit share/activity entities are required for authorization, notifications, and audit reporting.

## Phase 1 Constitution Re-check

- **I. Offline-First Training: PASS.** The design uses local SQLite/filesystem implementations and makes scanner availability explicit; it does not claim production malware protection.
- **II. Layered Architecture: PASS.** Data model, service contracts, storage/scanning infrastructure, and Blazor/HTTP presentation boundaries are documented separately.
- **III. Security by Authorization: PASS.** Authorization is applied before projection/byte access, and all caller-controlled path/name/identifier inputs are constrained.
- **IV. Verifiable Changes: PASS.** `quickstart.md` specifies focused integration scenarios for persistence rollback, scanner failure, IDOR/path traversal, notifications, and performance, plus build validation.
- **V. Simple, Maintainable Evolution: PASS.** The design preserves current keys and values, avoids full-text/cloud/distributed transaction infrastructure, and isolates future provider replacement behind existing-style interfaces.

**Phase 1 gate:** PASS. All research unknowns are resolved and design artifacts contain no unresolved markers.
