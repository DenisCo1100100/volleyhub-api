# Player and organizer game history

Personal history uses dedicated authenticated endpoints. Public discovery at `GET /api/games` keeps its existing contract.

| Endpoint | Item contract | Scope |
| --- | --- | --- |
| `GET /api/player-profiles/me/games` | `PlayerGameHistoryDto` | Games with a participation record for the current player profile |
| `GET /api/player-profiles/me/organized-games` | `OrganizedGameHistoryDto` | Games whose organizer is the current player profile |

Both endpoints require authentication and an active player profile. They return `401` for anonymous requests and `404` for a missing or deleted profile. The profile ID comes from the authenticated user; callers cannot select another player's history. A profile with no matching games receives an empty page with `200 OK`.

## Filters and pagination

Both endpoints accept these query parameters:

| Parameter | Default | Meaning |
| --- | --- | --- |
| `page` | `1` | One-based page number |
| `pageSize` | `20` | Items per page, from 1 to 100 |
| `period` | `All` | `All`, `Upcoming`, or `Past` |
| `status` | None | Exact game status: `Draft`, `Open`, `Full`, `Completed`, or `Cancelled` |
| `startsAtFrom` | None | Inclusive lower bound on the scheduled start |
| `startsAtTo` | None | Inclusive upper bound on the scheduled start |
| `courtId` | None | Exact court ID |

Player history also accepts `joinStatus`: `PendingApproval`, `Approved`, `Rejected`, `Cancelled`, `Removed`, or `Waitlisted`. It is independent of the game's `status`.

`Upcoming` includes games starting strictly after the server's current UTC time with status `Draft`, `Open`, or `Full`. It excludes completed and cancelled games even if their scheduled start is in the future. `Past` includes games starting at or before that time, regardless of status. Reaching the start time does not complete a game; a past open game remains visible as open. The clock is captured once for each request.

Filters are combined with AND. For example, `period=Upcoming&status=Cancelled` returns an empty page. Use `status=Completed` or `status=Cancelled` without a period to retrieve all games in that state. Offset-bearing date values are normalized to UTC. Invalid enum values, pagination, empty court IDs, inverted date ranges, and offsets beyond the supported integer range return `400` ProblemDetails.

Upcoming games are ordered by `startsAt ASC, id ASC`. All other queries use `startsAt DESC, id DESC`. The ID breaks ties deterministically. Offset pagination is stable for unchanged data; concurrent changes can move items between pages. The count and page are separate reads and do not promise a transactional snapshot.

Responses use the existing `PagedResult<T>` envelope: `items`, `page`, `pageSize`, `totalCount`, and `totalPages`. A page beyond the results is empty but retains the filtered total count.

Examples:

```http
GET /api/player-profiles/me/games?period=Upcoming&page=1&pageSize=20
GET /api/player-profiles/me/games?joinStatus=Cancelled
GET /api/player-profiles/me/organized-games?status=Completed
GET /api/player-profiles/me/organized-games?period=Past&status=Open
```

## Read models

Both item contracts contain `game`, using the existing `GameSummaryDto`: game ID, court location, organizer summary, schedule, capacity, price, level, join policy, status, approved/pending counts, available spots, and the current player's join status when applicable. Enum values are serialized as strings.

Player history adds `participation`: participant ID, join status, attendance status, offline payment status, joined/approved/cancelled/removed timestamps, and cancellation type. Every existing participation state remains visible by default, including withdrawals, rejections, removals, and waitlisted records. Organizing a game alone does not add it to participation history. An organizer who also joins a game can see it in both lists.

Organizer history adds per-game operational counts for waitlisted participants and approved participants with present, absent, or unmarked attendance. These counts expose the saved state; `NotMarked` does not mean absent. Available spots remain physical capacity, not a guarantee that a direct join can bypass the waitlist.

The organizer's `offlinePayments` has the same semantics as `GET /api/games/{id}/offline-payments`: only approved participants in paid open, full, or completed games contribute to expected payments. Paid participants contribute to the paid amount; the remaining expected participants contribute to the outstanding amount. Free, draft, and cancelled games have zero expected, paid, and outstanding totals. Cancelling a game does not erase a player's saved payment status. No payment processing or organizer statistics are introduced.

History reads current stored data, not immutable snapshots or an event log. Soft-deleted courts and organizer profiles remain available as historical references, so their deletion does not remove a game from a player's history. The caller must still have an active profile. Public discovery and all participation, cancellation, waitlist, attendance, and payment commands retain their existing behavior.

## Query execution and verification

Handlers resolve the current profile once and use repository projections. Each history repository call performs two database queries: a filtered count and a paged projection. Paging happens before related summaries and participant aggregates are selected. SQL joins provide court/organizer data; indexed participant subqueries provide counts and personal state. No participant collections or per-game repository calls are loaded, and the history projections do not track entities.

The existing indexes on organizer, start time, participant profile, and game/participant identity support these reads. No schema migration or new dependency is required.

Application tests cover authorization, profile requirements, filter forwarding, and validation. API tests cover personal isolation, participation states, operational totals, filters, pagination, deleted references, and JSON contracts using the project's EF InMemory fixture. Infrastructure tests verify PostgreSQL translation with `ToQueryString`, database-side filters/paging/aggregates, clock boundaries, and untracked projections. SQL translation checks do not measure production execution plans or database latency.
