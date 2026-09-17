# Quickstart Validation: Document Upload and Management

## Prerequisites

- macOS with the .NET 10 SDK.
- Repository root at `/Users/prashanttrivedi/TrainingProjects/ContosoDashboard`.
- A writable application-data directory configured outside `ContosoDashboard/wwwroot`.
- SQLite database created by the existing application startup and seeded users/projects.
- The test configuration must provide an offline malware scanner implementation or a deterministic test double. A missing/unavailable scanner must be tested as a rejection, never bypassed.

## Build and focused checks

```bash
cd /Users/prashanttrivedi/TrainingProjects/ContosoDashboard/ContosoDashboard
dotnet restore
dotnet build
```

Run the focused document service/integration test project when added by implementation. The test fixture should use SQLite and a temporary storage root, and should delete that root in teardown.

## Acceptance scenarios

1. Sign in as the seeded employee, upload a supported PDF with a title and category, and verify a per-file success result, progress feedback, a `Document` row, a generated relative storage key, and a file outside `wwwroot`.
2. Submit a mixed batch containing a valid file, an unsupported extension, and a file over 25 MiB. Verify the valid file succeeds and each invalid file has its own reason; neither rejected file appears in any list or search.
3. Configure the scanner to return `Infected`, `Unavailable`, and `Incomplete` in separate runs. Verify each run shows an actionable error, creates no usable document row, and leaves no temporary or final file.
4. Force a storage write or database failure after the other step. Verify compensating cleanup and that no row points to missing content; for replacement, verify the previous published file remains usable when the new version fails.
5. Seed personal, project, shared-user, shared-department, and unauthorized documents. Verify My Documents, project lists, search, preview, and download return only authorized results, including forged identifier and path traversal attempts.
6. As owner, project manager, team lead, and administrator, verify edit/replace/delete/share boundaries. Verify share and new-project-document operations create the expected in-app notifications and audit activities.
7. Attach an existing document and upload from a task. Verify task documents inherit the task's project and are visible only to authorized task/project viewers.
8. Verify the dashboard shows the current user's document count and five most recent uploads, and that empty personal/project/shared views render empty states.
9. Load a project with 500 documents and time list/search operations. Confirm the 95th percentile target is under 2 seconds; verify PDF/image preview under 3 seconds and a supported 25-MiB upload under 30 seconds under typical local test conditions.
10. As administrator, run a date-bounded activity report and verify uploads, downloads, previews, replacements, deletions, and shares are represented with document type, uploader, and access-pattern summaries.

## Expected security invariants

- No caller-supplied filename, path, or storage key is used to locate content.
- Unauthorized requests have no document metadata or byte disclosure.
- Scanner failure is a hard rejection with cleanup.
- Notifications and success audit events occur only after the underlying document operation succeeds.
