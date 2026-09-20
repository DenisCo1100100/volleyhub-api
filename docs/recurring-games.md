# Recurring games

## Schedule and generation

An organizer with an active player profile can create a weekly recurrence from any of their existing games, including a completed or cancelled game. The source is a configuration snapshot: court, capacity, offline price, level, join policy, description, and optional duration are copied. The source remains independent and is not counted among the generated occurrences. Its participants, waitlist, payments, attendance, status, and history are never copied. The source court must still exist and be active.

The first occurrence must start in the future. A required count of **2–52** bounds the schedule; the count includes the first generated game. All occurrences are created immediately in one transaction. There is no scheduler, background generation, indefinite recurrence, or extension endpoint. To continue a schedule, create another finite series from an existing game's configuration.

The first start is normalized to UTC. Each subsequent occurrence starts exactly seven days later, with the same optional duration. This is a fixed UTC schedule: local wall-clock time may change in regions observing daylight saving time. Time-zone rules, monthly patterns, arbitrary RRULEs, and automatic DST adjustments are outside this version. Minsk's UTC+03:00 schedule remains at the same local time throughout the year; the implementation is not tied to Minsk or any city.

Each occurrence is an ordinary `Game` initially in `Open` status. It uses the existing endpoints and rules for joining, approval, capacity, waitlisting, cancellation, offline payment recording, completion, attendance, and history. Players join each game separately.

## Identity and retries

The client generates a UUID `id` once per create operation and retains it when retrying. It becomes the recurrence ID. A repeated create with that ID returns `409 Conflict`; retrieve the recurrence to reconcile an uncertain response. Reusing the ID for a different payload also conflicts. Generating a different ID deliberately requests a different series, even if the configuration overlaps.

Every generated game stores immutable `recurrenceId` and one-based `occurrenceNumber`. PostgreSQL enforces a unique `(recurrence_id, occurrence_number)` index and a check constraint requiring both fields together. Individual rescheduling, cancellation, and completion retain this identity. A cancelled occurrence cannot be recreated in the same schedule. The recurrence primary key also protects concurrent create retries. All creation writes commit or roll back together.

## Editing and cancellation

| Operation | Effect |
| --- | --- |
| Existing `PUT /api/games/{id}` | Changes only that game under the existing lifecycle rules. May reschedule it; its original occurrence identity and the series schedule stay unchanged. |
| Update future games | Replaces court, capacity, price, level, join policy, and description for eligible occurrences at or after `fromOccurrenceNumber`. It also records those settings as the latest series configuration. |
| Existing `POST /api/games/{id}/cancel` | Cancels only that game. Other occurrences and the series remain active. |
| Cancel recurrence | Marks the series cancelled and cancels all its not-yet-started, nonterminal games. Repeating the operation is a successful no-op. |

Future selection uses each game's **current** `StartsAt > server UTC now`, including individually rescheduled games. An occurrence starting exactly now is excluded. Already started, completed, and cancelled games are skipped. The sequence boundary is inclusive and must be in the series; an update with no eligible games returns `409`. Cancelling a series with no remaining future games still closes the definition.

A future update explicitly replaces individual configuration overrides in the selected range. It preserves each game's current start/end times; changing the weekly schedule or duration as a batch is not supported. The read response exposes both original `scheduledStartsAt` and current `startsAt`, so rescheduled exceptions remain visible. Games before the sequence boundary are unchanged. The series configuration is the latest applied template, not a substitute for reading the actual details of an earlier occurrence.

All existing edit restrictions apply to every selected game. In particular, a `Full` game is not editable under the current `Game` lifecycle, capacity cannot fall below the approved count, and a price change is blocked if that game has recorded payments. If any selected game rejects the update, **none** of the selected games or series settings are saved. Price changes use the same participant payment synchronization as a single-game edit. The whole batch is saved once.

Series cancellation uses `Game.Cancel()` and preserves participation and payment records exactly like ordinary game cancellation. It does not cancel individual participation records, create late-cancellation penalties, refund payments, or change attendance. Completed games and their history remain available. Closing the series does not add a new lifecycle restriction to independent games that were skipped.

## HTTP contract

All recurrence endpoints require authentication, an active player profile, and ownership of the source game or recurrence. Recurrence definitions are organizer-only. The ordinary public game endpoints remain available for generated games.

| Action | Endpoint | Success |
| --- | --- | --- |
| Create and generate | `POST /api/game-recurrences` | `201 Created`, UUID body and `Location` |
| Read definition and ordered occurrences | `GET /api/game-recurrences/{id}` | `200 OK` |
| Update future settings | `PUT /api/game-recurrences/{id}/future` | `204 No Content` |
| Cancel series and future games | `POST /api/game-recurrences/{id}/cancel` | `204 No Content` |

Create request (replace UUIDs and choose a future date):

```json
{
  "id": "39c0f5c6-af97-465f-bde8-133f35dc6a16",
  "sourceGameId": "4444ea5d-8b5a-40a0-b7a1-773debb3e780",
  "firstStartsAt": "2027-01-04T17:00:00Z",
  "occurrenceCount": 8
}
```

Future update request:

```json
{
  "fromOccurrenceNumber": 3,
  "courtId": "6b3a381c-749b-4b0a-885d-b690eb01b788",
  "maxPlayers": 12,
  "pricePerPlayer": 25,
  "requiredLevel": "Intermediate",
  "joinPolicy": "ApprovalRequired",
  "description": "Weekly evening game"
}
```

The recurrence response includes `id`, `sourceGameId`, `organizerId`, `courtId`, `firstStartsAt`, nullable `duration` (a .NET TimeSpan JSON string such as `02:00:00`), `occurrenceCount`, the reusable settings, nullable `cancelledAt`, and an ordered `occurrences` array. Each occurrence has `gameId`, `occurrenceNumber`, `scheduledStartsAt`, `startsAt`, nullable `endsAt`, and `status`.

Game details, game summaries, and player/organizer history summaries expose nullable `recurrenceId` and `occurrenceNumber`; both are null for standalone and source games unless the source itself belongs to another series. Organizers can discover their series through the existing paged organized-games endpoint and retrieve the corresponding recurrence. A separate unbounded series listing is unnecessary.

Errors use ProblemDetails: `400` for invalid input or an out-of-range occurrence, `401` for missing authentication, `403` for another organizer's resource, `404` for a missing resource or active profile/court, and `409` for duplicate identities, closed recurrence, lifecycle restrictions, payment/capacity conflicts, or concurrent writes. Refresh the recurrence and affected games before retrying a conflict.

## Persistence and verification

Apply `AddRecurringGames` before deploying. It creates `game_recurrences`, adds nullable identity columns to existing games, and adds foreign keys, range/pair constraints, and the unique occurrence index. Existing games remain standalone. No participant data migration is needed.

Series updates use a recurrence version token; occurrence writes also use the existing game token that competes with participation writes. A stale single-game or participant change cannot silently overwrite a series operation, and competing series mutations cannot both commit from the same definition version. PostgreSQL saves each batch transactionally. No new dependencies are required.

Domain tests cover schedule bounds, UTC normalization, stable identity, selection, and independent lifecycle. Application tests cover authorization, one-save batches, payment restrictions, and cancellation. API tests cover the HTTP contracts and participation/payment/waitlist/attendance/history flow. Persistence tests verify stale-write detection with separate EF InMemory contexts and validate PostgreSQL model/migration metadata and SQL constraints. InMemory does not exercise PostgreSQL unique-index enforcement or transactional rollback; these require a running PostgreSQL instance.

Calendar integrations, notifications, venue synchronization, automatic payments, and complex recurrence rules are outside this feature.
