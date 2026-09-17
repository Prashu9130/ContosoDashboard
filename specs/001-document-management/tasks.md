---
description: "Executable task list for document upload and management"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `specs/001-document-management/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/document-management.md`, `quickstart.md`
**Tests**: Required by `spec.md`, `quickstart.md`, and the constitution's Verifiable Changes principle.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the focused test project and local configuration needed by all document stories.

- [X] T001 Create `tests/DocumentManagement.Tests.csproj` targeting `net10.0` with xUnit, EF Core SQLite, ASP.NET Core test hosting, and Playwright test dependencies.
- [X] T002 [P] Create `tests/GlobalUsings.cs` and `tests/TestAssembly.cs` with shared test imports and deterministic test-collection settings.
- [X] T003 [P] Create `tests/TestSupport/DocumentTestFixture.cs` with an isolated SQLite database, temporary storage root outside `ContosoDashboard/wwwroot`, seeded users/projects/tasks, and teardown cleanup.
- [X] T004 [P] Create `tests/TestSupport/FakeMalwareScanner.cs` and `tests/TestSupport/FakeFileStorageService.cs` with deterministic clean, infected, unavailable, incomplete, storage-failure, and persistence-failure controls.
- [X] T005 [P] Create `tests/Browser/DocumentManagementBrowserTests.cs` and configure a local host fixture for authenticated upload, list, preview, download, sharing, task, and dashboard acceptance flows.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared domain, persistence, infrastructure, authorization, and contract primitives before any story implementation.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T006 Add `ContosoDashboard/Models/Document.cs` with integer `DocumentId`, required trimmed `Title` max 255, optional `Description` max 2000, required text `Category` max 100 limited to Project Documents, Team Resources, Personal Files, Reports, Presentations, or Other, optional normalized `Tags` max 1000, display-only `OriginalFileName` max 255, generated relative `StorageKey` max 500, validated `FileType` max 255, normalized allowlisted `FileExtension` max 20, positive `FileSize` at most 25 MiB, required uploader/project/task foreign keys, and UTC upload/update timestamps.
- [X] T007 [P] Add `ContosoDashboard/Models/DocumentShare.cs` with integer key, document/actor/recipient fields, UTC `SharedDate`, exactly-one-of `UserId` or normalized `TeamName`, and duplicate-target protection metadata.
- [X] T008 [P] Add `ContosoDashboard/Models/DocumentActivity.cs` with immutable integer key, nullable deletion-compatible document reference, actor, text activity type, UTC timestamp, and bounded optional file/target/details fields for Upload, Replace, Download, Preview, Delete, Share, and ShareRevoked reports.
- [X] T009 Add `ContosoDashboard/Data/ApplicationDbContext.cs` document DbSets, relationships to `User`, `Project`, and `TaskItem`, cascade-safe share behavior, exactly-one-recipient and uniqueness constraints, indexes for `(UploaderId, UploadedDate)`, `(ProjectId, UploadedDate)`, `(Category, UploadedDate)`, `FileType`, and share target columns, and model validation for all data-model constraints.
- [X] T010 Add `ContosoDashboard/Services/DocumentContracts.cs` containing `IDocumentService`, `IFileStorageService`, `IMalwareScanner`, `IDocumentReportService`, upload/result DTOs, metadata/filter/sort/recipient/report types, and rejection codes `MetadataInvalid`, `UnsupportedType`, `FileTooLarge`, `AssociationUnauthorized`, `FileUnavailable`, `ScanInfected`, `ScanUnavailable`, `ScanIncomplete`, `StorageFailed`, and `PersistenceFailed`.
- [X] T011 [P] Add `ContosoDashboard/Services/FileStorageService.cs` implementing generated opaque relative keys below configured application-data storage outside `wwwroot`, safe extension/path validation, save/open/delete/exists operations, cancellation, and cleanup results without accepting caller-controlled paths or storage names.
- [X] T012 [P] Add `ContosoDashboard/Services/MalwareScanner.cs` implementing the offline scanner abstraction and explicit `Clean`, `Infected`, `Unavailable`, and `Incomplete` outcomes; unavailable or incomplete results must never be treated as clean.
- [X] T013 Add `ContosoDashboard/Program.cs` registrations and configuration for document services, protected storage root, scanner implementation, and the focused test seam while preserving offline mock authentication and existing services.
- [X] T014 Update `ContosoDashboard/appsettings.json` and `ContosoDashboard/appsettings.Development.json` with a writable application-data storage path outside `wwwroot`, 25 MiB limit, allowlisted MIME/extensions, and explicit training scanner configuration.
- [X] T015 Add `tests/Unit/DocumentModelConstraintTests.cs` to verify required/max-length/category/tag/extension/size constraints, integer identifiers, UTC timestamps, recipient exclusivity, share uniqueness, and document association invariants before story tests run.

**Checkpoint**: The test fixture, persistence model, service contracts, protected storage boundary, scanner outcomes, and configuration are ready for independent story work.

---

## Phase 3: User Story 1 - Upload and organize a work document (Priority: P1) MVP

**Goal**: Authenticated employees can submit one or more supported files with valid metadata, receive per-file progress/results, and retain only clean, consistently persisted documents.

**Independent Test**: Run the upload integration and browser tests against SQLite and temporary storage: a valid PDF succeeds and appears with metadata, mixed batches return independent errors, and infected/unavailable/incomplete/security/storage/database failures leave no usable row or file.

### Tests for User Story 1

- [X] T016 [P] [US1] Add `tests/Unit/UploadWorkflowTests.cs` covering independent per-file validation, supported PDF/Word/Excel/PowerPoint/text/JPEG/PNG allowlist, 25 MiB rejection, blank title/category/tag validation, progress reporting, and all required rejection reason codes.
- [X] T017 [P] [US1] Add `tests/Integration/DocumentUploadPersistenceTests.cs` covering clean scan publication order, generated relative storage keys outside `wwwroot`, metadata persistence, optional project/task authorization, scanner infected/unavailable/incomplete rejection, file-unavailable handling, storage failure cleanup, database failure cleanup, and no row pointing at missing content.
- [X] T018 [P] [US1] Add `tests/Browser/DocumentUploadAcceptanceTests.cs` covering authenticated multi-file selection, progress, per-file success/error results, clear size/type/scanner messages, and the document list metadata displayed after success.

### Implementation for User Story 1

- [X] T019 [US1] Implement `ContosoDashboard/Services/DocumentService.cs` upload validation and orchestration so each file validates metadata, MIME/extension, positive size at most 25 MiB, associations, and authorization before bounded temporary handling.
- [X] T020 [US1] Complete `ContosoDashboard/Services/DocumentService.cs` scan-before-publication workflow: write temporary/protected bytes, invoke `IMalwareScanner`, publish only `Clean`, return actionable `ScanInfected`, `ScanUnavailable`, or `ScanIncomplete`, and remove temporary/final bytes with no usable document row on every rejection/failure.
- [X] T021 [US1] Complete `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard/Models/Document.cs` persistence mapping for accepted upload metadata, uploader ownership, project/task association equality, generated storage key uniqueness, and UTC timestamps.
- [X] T022 [US1] Add `ContosoDashboard/Pages/Documents.razor` authenticated upload form with required title/category, optional description/project/tags, multi-file selection, progress display, per-file result/rejection messages, and list columns for title/category/date/uploader/size/type/project/tags.
- [X] T023 [US1] Add `ContosoDashboard/Shared/MainLayout.razor` navigation to the documents workflow and preserve authenticated access boundaries for the new page.
- [X] T024 [US1] Update `tests/DocumentManagement.Tests.csproj` and repository test execution configuration so the focused upload unit/integration/browser checks can run with SQLite and temporary storage.

**Checkpoint**: US1 is independently usable and testable; clean files are published with metadata, while every invalid or unsafe file produces a distinct actionable rejection and cleanup.

---

## Phase 4: User Story 2 - Find, view, and use authorized documents (Priority: P1)

**Goal**: Employees can search, filter, sort, preview, and download only documents authorized through ownership, project membership/management, explicit shares, team/department shares, or administrator responsibility.

**Independent Test**: Seed personal, project, user-shared, department-shared, and unauthorized documents; verify authorized list/search/project/shared results, sorting/filtering, PDF/image preview, downloads, forged identifiers, path traversal, and missing-content behavior.

### Tests for User Story 2

- [ ] T025 [P] [US2] Add `tests/Unit/DocumentAuthorizationTests.cs` covering ownership, project manager/member, explicit user share, matching team/department share, administrator access, unauthorized management exclusion, and authorization predicates applied before projection/materialization.
- [ ] T026 [P] [US2] Add `tests/Integration/DocumentSearchAndAccessTests.cs` covering `SearchAsync`, project/shared/recent/count queries, title/description/tags/uploader/project search, category/project/date filters, title/date/category/size sorting, `AsNoTracking`, and absence of unauthorized metadata.
- [ ] T027 [P] [US2] Add `tests/Integration/DocumentEndpointTests.cs` covering positive integer ID parsing, `404` for nonexistent/unauthorized documents, authorized download MIME/safe filename, PDF/JPEG/PNG-only preview, no caller path/storage key, missing/deleted bytes, and Download/Preview activity records.
- [ ] T028 [P] [US2] Add `tests/Browser/DocumentSearchPreviewDownloadAcceptanceTests.cs` covering list/filter/search/sort, project and shared views, authorized PDF/image preview under 3 seconds, accessible downloads, and empty states.

### Implementation for User Story 2

- [ ] T029 [US2] Implement reusable authorization predicates and query methods in `ContosoDashboard/Services/DocumentService.cs` for ownership, project membership/manager, explicit user/team share, administrator authority, and authorization-before-projection semantics.
- [ ] T030 [US2] Implement `ContosoDashboard/Services/DocumentService.cs` `SearchAsync`, `GetByIdAsync`, `GetProjectDocumentsAsync`, `GetSharedWithMeAsync`, `GetRecentAsync(count = 5)`, and `GetCountAsync` with required search fields, filters, sorts, `AsNoTracking`, and the 500-document/2-second query target.
- [ ] T031 [US2] Add `ContosoDashboard/Pages/DocumentEndpoints.cs` protected preview/download endpoints for `/documents/{documentId}/preview` and `/documents/{documentId}/download`, authenticating callers, calling the authorized service, returning safe MIME/display names, and never accepting filenames or storage paths.
- [ ] T032 [US2] Complete `ContosoDashboard/Pages/Documents.razor` list/search/filter/sort/preview/download UI with category/project/date controls, safe unavailable results, and no client-side authorization decisions.
- [ ] T033 [US2] Add `ContosoDashboard/Pages/SharedDocuments.razor` authorized “Shared with Me” results and empty state using `IDocumentService.GetSharedWithMeAsync`.

**Checkpoint**: US2 independently provides fast authorized retrieval; unauthorized metadata and bytes remain undisclosed even through forged IDs or traversal attempts.

---

## Phase 5: User Story 3 - Manage and share documents (Priority: P2)

**Goal**: Owners and authorized project managers can edit, replace, delete, and share documents; recipients are notified, audit records are created only after success, and administrators can report activity.

**Independent Test**: Exercise owner, project manager, team lead, and administrator boundaries for metadata changes, replacement, deletion, sharing, notifications, audit records, and date-bounded reports.

### Tests for User Story 3

- [ ] T034 [P] [US3] Add `tests/Unit/DocumentManagementAuthorizationTests.cs` covering owner/project-manager/administrator permissions, team-lead limits, unauthorized update/replace/delete/share denial, recipient target validation, and administrator-only report access.
- [ ] T035 [P] [US3] Add `tests/Integration/DocumentManagementTests.cs` covering metadata update, replacement scan/commit/old-file retention on failure, permanent deletion and cleanup failure reporting, share persistence/duplicate prevention, recipient and project notifications after success, and Upload/Replace/Delete/Share/ShareRevoked activity records.
- [ ] T036 [P] [US3] Add `tests/Integration/DocumentReportTests.cs` covering date-bounded administrator reports with upload/download/delete/share counts, document types, active uploaders, and access-pattern summaries.
- [ ] T037 [P] [US3] Add `tests/Browser/DocumentManagementAcceptanceTests.cs` covering owner edit/replace/delete confirmation, project-manager management, sharing to users/teams, recipient notification, “Shared with Me,” and administrator report access.

### Implementation for User Story 3

- [ ] T038 [US3] Implement `UpdateMetadataAsync`, `ReplaceFileAsync`, `DeleteAsync`, and `ShareAsync` in `ContosoDashboard/Services/DocumentService.cs` with narrow management authorization, metadata validation, association validation, generated replacement keys, clean-scan gating, and compensating cleanup.
- [ ] T039 [US3] Implement deletion consistency in `ContosoDashboard/Services/DocumentService.cs`: remove documents from user-visible queries, delete protected bytes, surface cleanup failure for repair instead of claiming success, and preserve deletion audit semantics with a nullable document reference.
- [ ] T040 [US3] Implement `ContosoDashboard/Services/NotificationService.cs` integration for recipient share and eligible project-member notifications only after the database operation succeeds.
- [ ] T041 [US3] Implement `ContosoDashboard/Services/DocumentReportService.cs` administrator authorization and date-bounded activity/type/uploader/access-pattern aggregation using `AsNoTracking`.
- [ ] T042 [US3] Add management and sharing controls to `ContosoDashboard/Pages/Documents.razor`, including edit, replacement, delete confirmation, user/team recipient selection, actionable errors, and operation status.
- [ ] T043 [US3] Add administrator report UI to `ContosoDashboard/Pages/DocumentReports.razor` with date filters and the contract's document type, active uploader, and access-pattern summaries.

**Checkpoint**: US3 independently enforces management boundaries, keeps storage/metadata consistent across failure, notifies only after success, and exposes audit reporting only to administrators.

---

## Phase 6: User Story 4 - Connect documents to work in the dashboard (Priority: P2)

**Goal**: Employees can attach/upload documents from tasks, project members inherit authorized project context, and the dashboard shows document count and five recent uploads.

**Independent Test**: Attach an existing document and upload from a task, verify task/project association and viewer authorization, trigger project notifications, and verify dashboard count/recent/empty-state behavior.

### Tests for User Story 4

- [ ] T044 [P] [US4] Add `tests/Integration/TaskDocumentAssociationTests.cs` covering existing-document attachment, task upload, task accessibility, task-project equality, unauthorized project association rejection, and project/task viewer authorization.
- [ ] T045 [P] [US4] Add `tests/Integration/DashboardDocumentSummaryTests.cs` covering current-user count, exactly five most recent uploads, ordering, authorized visibility, and empty personal/project/shared results.
- [ ] T046 [P] [US4] Add `tests/Browser/TaskAndDashboardDocumentAcceptanceTests.cs` covering task attach/upload, project-context display, project-member visibility, notification, dashboard summary, and empty states.

### Implementation for User Story 4

- [ ] T047 [US4] Extend `ContosoDashboard/Services/DocumentService.cs` with task attachment/upload operations that require an accessible task, reject forged or unauthorized associations, and enforce `TaskId` project equality with `ProjectId`.
- [ ] T048 [US4] Update `ContosoDashboard/Pages/Tasks.razor` and `ContosoDashboard/Pages/ProjectDetails.razor` with authorized existing-document attachment, task-context upload, project document display, and actionable association errors.
- [ ] T049 [US4] Extend `ContosoDashboard/Services/DashboardService.cs` with authorized document count and five-most-recent-upload queries while preserving existing dashboard summaries.
- [ ] T050 [US4] Update `ContosoDashboard/Pages/Index.razor` to display the current user's document count and five most recent authorized uploads with an empty state.
- [ ] T051 [US4] Complete project-document notification logic in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/NotificationService.cs` for eligible project members after successful project upload/association.

**Checkpoint**: US4 independently connects documents to tasks/projects and the dashboard without bypassing inherited authorization.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate the complete feature against the documented performance, security, offline, and documentation requirements.

- [ ] T052 [P] Add `tests/Integration/DocumentPerformanceTests.cs` loading up to 500 documents per project and recording the 95th-percentile list/search target under 2 seconds, PDF/image preview under 3 seconds, and supported 25 MiB upload under 30 seconds under typical local conditions.
- [ ] T053 [P] Add `tests/Integration/DocumentSecurityInvariantTests.cs` verifying no caller filename/path/storage key controls lookup, zero unauthorized preview/download/search/management disclosure, scanner failure cleanup, and success-only notifications/audit events.
- [ ] T054 [P] Update `README.md` with offline storage configuration, protected storage/scanner limitations, focused test commands, browser acceptance prerequisites, and explicit training-only mock scanner/authentication limitations.
- [ ] T055 Run `dotnet restore` and `dotnet build` from `ContosoDashboard/` and resolve feature-caused errors or document pre-existing warnings in the implementation change notes.
- [ ] T056 Run the focused unit/integration/browser test commands from `tests/DocumentManagement.Tests.csproj`, execute every `quickstart.md` acceptance scenario, and record any environment-dependent timing limitations.
- [ ] T057 Review `ContosoDashboard/Program.cs`, `ContosoDashboard/Data/ApplicationDbContext.cs`, `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Pages/DocumentEndpoints.cs`, and all document tests for constitution compliance and no uploaded content under `ContosoDashboard/wwwroot`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; T001 must establish the test project before test fixture tasks T002-T005 can compile.
- **Foundational (Phase 2)**: Depends on Phase 1; T006-T014 establish the shared contracts and infrastructure, then T015 verifies model constraints. This phase blocks all stories.
- **User Stories (Phases 3-6)**: Depend on Phase 2. US1 and US2 are both P1 and can proceed in parallel after the foundation; US3 and US4 are P2 and can proceed after the foundation, with US3 using the sharing/activity primitives and US4 using the completed service/query surface from US1/US2.
- **Polish (Phase 7)**: Depends on the desired user stories being complete; T052-T057 validate the integrated feature.

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 2 only. It is the MVP and creates the clean published documents consumed by later stories.
- **US2 (P1)**: Depends on Phase 2 and can use seeded documents or US1 fixture helpers; it must remain independently testable with seeded data.
- **US3 (P2)**: Depends on Phase 2 and the document service/model contracts; its tests should seed documents rather than require US1 UI completion.
- **US4 (P2)**: Depends on Phase 2 and the task/project associations; its tests should seed documents and tasks rather than require US1-3 browser completion.

### Within Each User Story

- Write focused tests before implementation and confirm they fail for the missing behavior.
- Implement models/contracts before service orchestration, services before pages/endpoints, and core behavior before browser integration.
- Keep authorization at the service/data boundary and apply it before projection or byte access.
- Complete and independently validate each story checkpoint before moving to the next priority.

### Parallel Opportunities

- After T001, T002-T005 can run in parallel because they create separate test-support files.
- After shared model/contract work is available, T007, T008, T011, and T012 can run in parallel in separate files; T009 and T013 depend on their relevant types.
- In US1, T016-T018 can run in parallel because they target separate test files; T019-T020 then implement the shared upload workflow.
- In US2, T025-T028 can run in parallel because they target separate test files; the P1 US1 and US2 story teams can also work in parallel after Phase 2.
- In US3, T034-T037 can run in parallel; report implementation T041 can proceed separately from page work after contracts exist.
- In US4, T044-T046 can run in parallel; task pages and dashboard pages can proceed in parallel after their service methods exist.
- T052-T054 can run in parallel during polish because they target separate files.

## Parallel Example: P1 Stories

```text
After Phase 2 completes:
  Track A: T016-T024 for US1 upload and publication
  Track B: T025-T033 for US2 search, authorization, preview, and download
```

## Parallel Example: P2 Stories

```text
After the shared document contracts exist:
  Track A: T034-T043 for US3 management, sharing, notifications, and reports
  Track B: T044-T051 for US4 task/project/dashboard integration
```

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 test infrastructure.
2. Complete Phase 2 models, persistence, storage, scanner, contracts, and configuration.
3. Complete Phase 3 US1 tests and implementation.
4. Run the US1 service/integration/browser checks and confirm clean publication plus rejection cleanup.
5. Stop for an MVP review before expanding retrieval, management, and dashboard workflows.

### Incremental Delivery

1. Add US2 to make the clean repository searchable and safely consumable.
2. Add US3 for lifecycle management, sharing, notifications, and audit reports.
3. Add US4 for task/project/dashboard context.
4. Run Phase 7 performance, security, quickstart, and build validation before release.

### Completion Criteria

- Every task above follows `- [ ] T### [P?] [US#?] description with an exact file path`.
- Every user story has an independent test criterion and focused unit/integration/browser verification.
- All contract methods, data-model constraints, rejection codes, security invariants, and quickstart scenarios are assigned to tasks.
- No task instructs the implementer to place uploaded content under `ContosoDashboard/wwwroot`.