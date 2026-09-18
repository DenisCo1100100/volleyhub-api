# Game Participant Cancellation Lifecycle

This document defines the cancellation contract for an approved `GameParticipant` before waitlists, capacity expansion, and reliability scoring are implemented.

## Scope

The lifecycle is generic for a sports game. It applies to a participant and a scheduled game, not to volleyball-specific positions, formats, or scoring.

This decision does not introduce waitlists, reliability scores, notifications, refunds, in-app payments, coach rules, or venue reservation behavior.

## Current model

The current domain model contains two separate dimensions:

* `GameParticipantJoinStatus`: `PendingApproval`, `Approved`, `Rejected`, or `Cancelled`;
* `GameParticipantAttendanceStatus`: `NotMarked`, `Present`, or `Absent`.

`Leave` currently changes the join status to `Cancelled` and may reopen a full game. The cancellation operation does not currently distinguish a normal cancellation from a late cancellation, persist a cancellation timestamp, or record who initiated it.

Attendance is marked by the organizer after the game is completed. It must remain independent from cancellation: a cancellation is a decision to release participation, while attendance is an observation of whether an approved participant attended.

## Approved place ownership

An approved participant owns one confirmed place from `ApprovedAt` until one of these events occurs:

1. the participant cancels;
2. the organizer cancels the participant on the participant's behalf;
3. the game starts, after which participation can no longer be released for capacity purposes.

A pending request does not own a confirmed place. A rejected or cancelled participant does not own a place.

## Cancellation threshold

For the initial implementation, the global late-cancellation threshold is **24 hours before `Game.StartsAt`**.

* A cancellation requested before `StartsAt - 24 hours` is an **on-time cancellation** and has no reliability consequence.
* A cancellation requested at or after `StartsAt - 24 hours` and before `StartsAt` is a **late cancellation**.
* The threshold is based on the game's scheduled start time and the server's UTC time source.
* The threshold is a domain policy, not a game field. It should be represented so that a future release can configure it per game without changing the lifecycle terminology.

The participant's local timezone does not affect the threshold calculation. `Game.StartsAt` and all persisted event timestamps use `DateTimeOffset`.

## Lifecycle before and after game start

The target lifecycle for an approved participant is:

```text
PendingApproval -> Approved -> Cancelled
PendingApproval -> Rejected
Approved -> Present      (attendance, after the game)
Approved -> Absent       (attendance, after the game)
```

The cancellation classification is derived from the cancellation time:

```text
Approved + cancellation before the threshold -> OnTimeCancellation
Approved + cancellation at/after the threshold and before start -> LateCancellation
```

Cancellation after `Game.StartsAt` is not a new capacity release and is not a way to avoid attendance handling. It is recorded as a late cancellation only if the product later chooses to allow post-start cancellation recording; the initial cancellation command should reject a participant-initiated cancellation after start. A participant who has not cancelled before the start remains eligible for attendance marking.

A no-show is not a cancellation. An approved participant becomes a no-show only when the organizer marks the participant `Absent` after the game is completed. No-show determination is therefore an attendance outcome and is not available before the game starts.

## Relationship between cancellation and attendance

Cancellation and attendance are independent concerns:

* `JoinStatus = Cancelled` means that the participant no longer participates in the game.
* `AttendanceStatus = NotMarked` means that attendance has not been recorded; it is not a no-show.
* `AttendanceStatus = Present` or `Absent` applies only to an approved participant who remained in the participation lifecycle through the game.
* A cancelled participant must not later be marked `Present` or `Absent`.
* An approved participant must not be cancelled after attendance has been recorded.
* `Absent` is the attendance representation of a no-show. A separate `NoShow` join or cancellation status is not required.

## Who can cancel

A participant can cancel their own participation while the game has not started.

The organizer can cancel a participant before the game starts on the participant's behalf. The organizer action uses the same on-time/late classification based on the effective cancellation timestamp, but persists the actor separately so the product can distinguish self-cancellation from organizer removal later.

Organizer cancellation does not mean that the participant was absent. It ends participation; attendance must not be marked for that participant.

The initial implementation does not define a general administrative override or a cancellation reason taxonomy. If a reason is needed later, it can be added without changing the lifecycle states.

## Persisted data

The minimum data required on the participation record is:

* `JoinedAt` — when the join request was created;
* `ApprovedAt` — when the confirmed place was granted;
* `CancelledAt` — when participation was cancelled, nullable;
* `CancellationType` — none, on-time, or late, nullable while the participant is active;
* `CancellationActor` — participant or organizer, nullable while the participant is active;
* the existing `JoinStatus` and `AttendanceStatus`.

All timestamps are immutable event timestamps stored as `DateTimeOffset` in UTC. `CancelledAt` is written once and is not replaced by later edits.

A separate cancellation-history table is not required for the first implementation because a participation record can be cancelled at most once. If rejoining or repeated participation attempts become supported, each attempt must have its own participation record or an append-only cancellation history; overwriting `CancelledAt` is not acceptable.

## Capacity behavior

An on-time or late cancellation made before `Game.StartsAt` releases the confirmed place immediately. In the current capacity model, a full game may be reopened so another participant can join.

Cancellation after the game starts does not release capacity and must not reopen the game. Capacity is a pre-start concept; waitlist promotion, if added later, must only consume places released before the start.

Pending, rejected, and cancelled records do not count toward approved capacity. An approved participant counts until the cancellation is accepted before game start.

## Rejoining the same game

A participant cannot rejoin the same game after cancelling in the initial lifecycle. The existing one-participant-per-game identity constraint remains valid, and a cancelled record is retained as the participation history.

If rejoining is required later, it must be designed together with participation-attempt history and capacity semantics rather than by resetting `JoinStatus` or overwriting cancellation data.

## Implementation contract

The implementation issue that follows this decision must:

1. preserve the separation between join/cancellation state and attendance state;
2. classify cancellation using the global 24-hour threshold and the effective cancellation timestamp;
3. persist cancellation type, timestamp, and actor;
4. reject participant cancellation after the game starts;
5. release capacity only for accepted cancellation before the start;
6. prevent attendance from being recorded for cancelled participants;
7. keep the model generic so a future per-game threshold or other sports can be introduced without renaming the lifecycle.
