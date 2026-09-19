# Game waitlist

## Participation and ordering

An authenticated player with an active profile can explicitly join the waitlist of a full game before `StartsAt`. The game must use `Open` or `ApprovalRequired`. A player with any existing participation record cannot join again, including after withdrawal, rejection, cancellation, or removal. Ordinary joining keeps its existing contract and does not automatically join the waitlist.

`Waitlisted` is a participant join status. It does not consume capacity, require offline payment, or permit attendance marking. `JoinedAt` records entry into the queue. The queue is ordered by `JoinedAt`, then participant `Id` ascending to break timestamp ties deterministically. Positions are one-based and are recalculated after transitions.

The player can leave through the existing leave endpoint. The organizer can dismiss a waitlisted player through the existing reject endpoint. These transitions retain the participation record without recording cancellation timestamps or late-cancellation penalties because no confirmed place was held.

## Promotion

Promotion is explicitly controlled by the organizer. Each request promotes the first waitlisted player with an active profile to `Approved`, provided the game is open, has a free place, and has not started. Deleted profiles are skipped. Other waitlisted records retain their relative ordering. The organizer can reject a queued player before promoting the next one.

For `ApprovalRequired`, promotion itself grants approval; there is no second approval step. There are no temporary offers, claim deadlines, notifications, or background promotion jobs. The price at promotion determines the initial offline payment state. A later organizer change to `InviteOnly` does not invalidate the existing queue or prevent organizer-controlled promotion.

Cancellation or removal of an approved participant releases a place and reopens a full game. It does not automatically promote anyone. While waitlisted records remain, ordinary direct joining and approval cannot consume the released places. Pending approval requests may still be submitted under `ApprovalRequired`, but waitlist promotion has priority. Joining the waitlist itself requires a full game. Once the queue is empty, normal joining and approval resume.

Only approved participants count toward capacity. Promotion fills one place and marks the game `Full` if it reaches `MaxPlayers`. Editing capacity below the approved count is rejected. Cancelled, completed, and started games cannot accept waitlist entries or promotions. Withdrawing or rejecting an unconfirmed waitlist record remains possible after the game closes.

## HTTP contract

| Action | Endpoint | Success |
| --- | --- | --- |
| Join waitlist | `POST /api/games/{gameId}/waitlist` | `201 Created`, participant ID |
| Promote next eligible player (organizer) | `POST /api/games/{gameId}/waitlist/promote` | `200 OK`, promoted participant ID |
| Withdraw own entry | `POST /api/games/{gameId}/participants/leave` | `204 No Content` |
| Reject entry (organizer) | `POST /api/game-participants/{participantId}/reject` | `204 No Content` |
| Read participation and queue positions | `GET /api/games/{gameId}/participants` | Existing list with nullable `waitlistPosition` |
| Read game details | `GET /api/games/{gameId}` | Existing details with `waitlistedParticipantCount` and positions in `participants` |

`currentUserJoinStatus` in game details and game summaries may now be `Waitlisted`. `waitlistPosition` is null for records outside the queue. Existing approved/pending counts and `availableSpots` exclude waitlisted players. Available spots represent physical capacity; while the queue exists they are assigned through promotion.

Failures use ProblemDetails: `400` for invalid IDs, `401` for unauthenticated requests, `403` for another player's organizer operation, `404` for missing games/profiles, and `409` for lifecycle, capacity, duplicate-entry, empty-queue, or concurrent modification conflicts. Clients should refresh the game before retrying a concurrency conflict.

## Persistence and concurrency

The existing participant table and unique `(game_id, player_profile_id)` index remain in use. `Waitlisted` is stored through the existing string enum conversion; no queue table is needed.

The `AddGameWaitlistConcurrency` migration adds a game version token. Each game edit and every participation write updates this token, including writes that leave the game's status unchanged. Competing capacity decisions therefore cannot both commit from the same game version. Participant join status is also an EF concurrency token, protecting records read before a newer game version was loaded. PostgreSQL saves the game and participant changes in one transaction, rolling back the entire losing operation. Duplicate participation conflicts are also returned as `409`.

Apply the migration before deploying this feature. Domain and application tests cover the policy; API tests cover the HTTP flow. Persistence tests use independent EF InMemory contexts to force stale writes and verify the PostgreSQL model and migration metadata. InMemory tests do not verify PostgreSQL transaction rollback or locking behavior.
