# Contributing

This document describes how to work on VolleyHub API without breaking the project structure.

## Project architecture

VolleyHub API follows Clean Architecture.

Project responsibilities:

* `VolleyHub.Domain` — domain entities, enums and business rules.
* `VolleyHub.Application` — use cases, commands, queries, DTOs, validators and abstractions.
* `VolleyHub.Infrastructure` — EF Core, PostgreSQL, repositories and external integrations.
* `VolleyHub.Api` — HTTP controllers, request handling, API configuration and exception handling.

## Dependency rules

Keep dependencies pointing inward.

Allowed:

* `Application` can reference `Domain`.
* `Infrastructure` can reference `Application` and `Domain`.
* `Api` can reference `Application` and `Infrastructure`.

Not allowed:

* `Domain` must not reference `Application`, `Infrastructure` or `Api`.
* `Application` must not reference `Infrastructure` or `Api`.
* Domain entities must not use EF Core attributes.
* API controllers must not access EF Core directly.
* Infrastructure code must not leak into Application handlers.

## Domain rules

Domain entities should contain business rules and state changes.

Examples:

* `Court.Create(...)`
* `Court.Update(...)`
* `Court.Delete()`
* `Game.Create(...)`
* `Game.Cancel()`
* `Game.Complete()`

Use private setters where possible.

Validate required business data inside domain entities.

Do not inject services into domain entities unless there is a strong reason.

## Application rules

Application layer contains use cases.

Use MediatR commands and queries:

* `CreateXCommand`
* `UpdateXCommand`
* `DeleteXCommand`
* `GetXByIdQuery`
* `GetXsQuery`

Handlers should use abstractions:

* repository interfaces
* `IUnitOfWork`
* other application-level interfaces

Handlers should not use:

* `DbContext`
* EF Core
* ASP.NET Core types
* controller-specific logic

Validation should be done with FluentValidation where it belongs to request/input validation.

Domain validation should stay in Domain.

## Infrastructure rules

Infrastructure implements Application abstractions.

EF Core configuration should stay in Infrastructure.

Use separate configuration classes for entities:

* `CourtConfiguration`
* `GameConfiguration`

Repositories should hide EF Core details from Application.

Migrations belong to Infrastructure.

## API rules

Controllers should be thin.

Controllers may:

* receive HTTP requests
* call MediatR
* return HTTP responses

Controllers should not:

* contain business rules
* query DbContext directly
* duplicate domain logic

Use `ProblemDetails` for API errors where possible.

## Tests

Add or update tests when changing behavior.

Expected test locations:

* Domain tests: `tests/VolleyHub.Domain.UnitTests`
* Application tests: `tests/VolleyHub.Application.UnitTests`

Run all tests before pushing:

```bash
dotnet test
```

## Branch naming

Use short branch names with a clear prefix:

* `feat/...` for new features
* `fix/...` for bug fixes
* `docs/...` for documentation
* `test/...` for tests only
* `chore/...` for maintenance

Examples:

```text
feat/games-domain-model
feat/games-application-use-cases
fix/exclude-deleted-courts
docs/contribution-guidelines
```

## Commit messages

Use short English commit messages.

Examples:

```text
feat: add games domain model
feat: add games application use cases
fix: exclude deleted courts from queries
docs: add contribution guidelines
test: cover court text length validation
chore: polish courts module
```

## Pull requests

Every PR should be focused.

Do not mix unrelated changes in one PR.

Good PR:

* adds one feature
* fixes one bug
* updates related tests
* has a short summary

Bad PR:

* changes Domain, API, unrelated formatting, Docker and migrations without one clear reason

PR description should include:

```md
## Summary

- Added ...
- Updated ...
- Tested ...

Closes #issue-number
```

## Before pushing

Check:

```bash
dotnet test
git status
```

Only expected files should be changed.

## Before merging

Check:

* PR has a clear title.
* PR has a short summary.
* CI is green.
* Files changed are expected.
* No unrelated formatting changes.
* No architecture rule is broken.

## Sports extensibility rule

VolleyHub MVP is volleyball-first, but shared domain code should not be hardcoded as volleyball-only unless the feature explicitly requires it.

When adding or changing shared domain models, avoid unnecessary volleyball-specific naming and rules.

Prefer generic names for shared concepts:

* `Game`, not `VolleyballGame`;
* `Participant`, not `VolleyballPlayer`;
* `Court` or another generic place concept, not a volleyball-only place model;
* configurable limits and formats instead of hardcoded volleyball-only limits.

Volleyball-specific concepts may be introduced later, but they should be isolated from the shared core when possible.

Good:

* common game lifecycle;
* common participant flow;
* common attendance tracking;
* common venue and reservation flow.

Be careful with:

* sport-specific positions;
* sport-specific scoring;
* sport-specific team formats;
* hardcoded player limits;
* volleyball-only validation inside shared entities.

