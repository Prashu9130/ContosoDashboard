<!--
Sync Impact Report
Version change: unversioned scaffold -> 1.0.0
Modified principles: template placeholders -> five ContosoDashboard principles
Added sections: Technology and Security Constraints; Development Workflow and Quality Gates
Removed sections: none
Follow-up TODOs: Confirm the original constitution ratification date.
-->

# ContosoDashboard Constitution

## Core Principles

### I. Offline-First Training
ContosoDashboard MUST run locally without cloud services or production credentials. New
features MUST preserve the training application's offline workflow and MUST use local
implementations behind abstractions where a future cloud implementation is anticipated.
This keeps exercises reproducible, inexpensive, and safe to run in a classroom environment.

### II. Layered Architecture
Features MUST keep presentation, business services, data access, and infrastructure
responsibilities separate. Shared behavior MUST be exposed through focused interfaces when
multiple implementations or future migrations are plausible. Business rules MUST NOT be
duplicated in UI components or bypass service-layer authorization.

### III. Security by Authorization
Every authenticated workflow MUST enforce authorization at the service or data-access
boundary, including ownership, role, and project-membership checks. User-controlled paths,
filenames, identifiers, and uploaded content MUST be validated before use. Training mock
authentication MUST remain clearly non-production, and security-sensitive changes MUST
include a test or explicit verification of the relevant access-control behavior.

### IV. Verifiable Changes
Each change MUST include the narrowest practical automated test, build, or validation that
can disprove its intended behavior. Changes affecting persistence, authorization, file
handling, or cross-layer contracts MUST include integration-level verification where the
local architecture supports it. A change is not complete until the project builds and
known warnings or limitations are reported rather than hidden.

### V. Simple, Maintainable Evolution
The implementation MUST favor the smallest design that satisfies the documented need,
reuse existing project conventions, and avoid speculative infrastructure. Public contracts,
database keys, stored data formats, and migration assumptions MUST change only deliberately
with documented compatibility impact. New complexity MUST have a concrete requirement or
measurable reliability, security, or maintainability benefit.

## Technology and Security Constraints

The application uses ASP.NET Core with Blazor Server, Entity Framework Core, and a local
development database. The current training stack targets .NET 10 and uses SQLite for local
persistence. Data files and uploaded content MUST remain outside web-accessible directories
unless an authorized endpoint explicitly mediates access. File uploads MUST use allowlists,
size limits, generated storage names, and an upload sequence that prevents orphaned records.
Production identity, malware scanning, cloud storage, and compliance controls are outside the
training implementation and MUST NOT be implied by mock behavior.

## Development Workflow and Quality Gates

Feature work MUST begin with a concise specification of user behavior, authorization rules,
data impact, and offline constraints. Implementation MUST preserve existing architectural
boundaries and update relevant documentation when behavior or setup changes. Before review,
contributors MUST run the focused tests or checks for the changed behavior and a project build;
reviewers MUST inspect authorization paths, persistence changes, input validation, and
unintended production assumptions. Unresolved warnings, test gaps, and intentional training
limitations MUST be documented in the change description.

## Governance

This constitution governs project decisions and supersedes informal practices when they
conflict. Amendments MUST be made through a documented change to this file, include a Sync
Impact Report, explain affected principles or sections, and update the version and amendment
date. Versioning follows semantic rules: MAJOR for backward-incompatible governance changes,
MINOR for new or materially expanded principles or sections, and PATCH for clarifications or
non-semantic wording changes. Every feature review MUST check compliance with these rules;
exceptions MUST state their rationale and scope. The constitution MUST be reviewed whenever
the technology stack, deployment assumptions, security model, or training purpose changes.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): original adoption date is not recorded | **Last Amended**: 2026-09-18
