# VolleyHub Product Vision

## 1. Purpose

VolleyHub is a platform for organizing and joining real-life volleyball games.

The primary goal is to make the full lifecycle of an amateur volleyball game easier:

1. an organizer prepares a game;
2. players reserve places;
3. the organizer manages the participant list;
4. a coach may be involved when needed;
5. the game takes place;
6. attendance is recorded;
7. participation history and reliability are updated.

The product should reduce the amount of coordination that currently happens manually through chats, spreadsheets, direct messages, and bank transfers.

The first priority is not automatic venue booking.

The first priority is making organizer-led games work extremely well.

## 2. Initial Market

The initial market is Belarus.

The practical starting point is Minsk, but the product should not be technically limited to Minsk.

Other cities in Belarus should be able to use VolleyHub when there are active organizers, players, coaches, and places to play.

The architecture and domain model should not depend on one specific city.

## 3. Current Product Focus

The current product focus is the organizer-led game.

In the current model, an organizer already has an arrangement with a court, gym, school hall, sports hall, or another place outside VolleyHub.

For example, the organizer may already know:

* where the game will happen;
* when the court is available;
* how much the rental costs;
* how the venue expects to be paid;
* who to contact at the venue.

VolleyHub does not need to automate that relationship yet.

Instead, VolleyHub helps the organizer turn that arrangement into a real game with a complete participant lifecycle.

The core product question for the current phase is:

> Can an organizer create a game, fill the required places, manage players and coaches, run the game, record attendance, and understand what happened afterwards without needing external organizational tools?

That flow should be brought to a high level of completeness before the platform expands deeply into venue management.

## 4. Core User Types

### 4.1 Players

Players are users who want to find and join volleyball games.

A player should be able to:

* create and maintain a player profile;
* browse available games;
* see game location, time, level, price, organizer, and available places;
* reserve a place;
* request organizer approval when required;
* leave or cancel participation;
* see their participation state;
* see their game history;
* build attendance history;
* build reliability over time.

A player may also organize games.

Being an organizer does not necessarily require a separate authentication role.

For the current product, organizing a game is primarily a capability of a player profile.

### 4.2 Organizers

Organizers are people who take responsibility for making a game happen.

An organizer usually already has an external arrangement with the place where the game will be played.

An organizer should be able to:

* choose or add the court where the game will happen;
* create a game;
* define start and end time;
* define maximum capacity;
* define level requirements;
* define join policy;
* define offline price per player;
* describe the game;
* open the game for participation;
* see who reserved places;
* approve or reject requests;
* remove participants when necessary;
* manage cancellations;
* understand how many places remain;
* track offline payment state;
* manage coaches when the game requires them;
* complete the game;
* mark attendance;
* review the result of the game afterwards.

Organizers are one of the most important product users.

Without organizers there are no games for players to join.

VolleyHub should remove as much repetitive organizational work from them as possible.

Possible organizer features later:

* organizer dashboard;
* organizer history;
* statistics;
* average attendance;
* cancellation statistics;
* organizer reputation;
* badges;
* preferred player lists;
* blocked player lists;
* automated reminders;
* venue benefits after venue integration is introduced.

### 4.3 Coaches

Coaches are people who may participate in games or conduct training sessions.

Not every game requires a coach.

For games where a coach is required, VolleyHub should eventually support assigning or reserving the required coach capacity.

The exact model is intentionally not fixed yet.

Possible approaches include:

* the organizer assigns a coach;
* the coach requests to join a game;
* a game contains dedicated coach places;
* a coach consumes one of the general game places;
* coaches use a separate participation model from players.

This should be decided based on real organizer and coach workflows rather than guessed too early.

A coach may also be a regular player.

A dedicated `CoachProfile` is likely useful later because coaches may need information that does not belong in a normal player profile, such as:

* coaching experience;
* specialization;
* description;
* training focus;
* qualifications;
* preferred levels;
* price;
* training history.

## 5. Courts and Venues in the Current Phase

A `Court` currently represents the physical place where a game happens.

It may be:

* an outdoor public court;
* a community court;
* a school gym;
* a rented sports hall;
* a private gym;
* another suitable volleyball location.

The important distinction in the current product is not whether VolleyHub manages the venue.

The organizer may already have access to a paid or managed court through an external agreement.

Therefore, a court can be used in VolleyHub even when the venue itself does not have an account in VolleyHub.

Current court information may include:

* name;
* address;
* city;
* latitude;
* longitude;
* surface type;
* indoor or outdoor type;
* description.

Venue ownership and automated venue availability are deferred to a later product stage.

The data model should remain compatible with linking a court to a managed venue in the future.

## 6. Main Organizer-Led Game Flow

This is the primary product flow for the current development stage.

1. The organizer arranges a court or hall outside VolleyHub.
2. The organizer selects an existing court or adds the place to VolleyHub.
3. The organizer creates a game.
4. The organizer defines the date, time, capacity, level, price, join policy, and other details.
5. The game becomes available to players.
6. Players reserve places or request approval.
7. The organizer approves, rejects, or removes participants when required.
8. Available capacity is updated as places are taken or released.
9. If the game requires a coach, the coach is assigned or reserves the appropriate place according to the future coach participation rules.
10. The organizer tracks the participant list and offline payment state.
11. The game takes place.
12. The organizer completes the game.
13. The organizer marks who was present and who was absent.
14. Attendance history and reliability are updated.
15. The organizer and players can review the completed game afterwards.

This lifecycle is more important than advanced platform features.

The product should make this flow reliable before expanding into venue automation, payments, social networking, or other secondary domains.

## 7. Games

A `Game` is the central organizational unit of the current product.

A game has:

* organizer;
* court;
* start time;
* optional end time;
* maximum number of players;
* offline price per player;
* required level;
* join policy;
* participant list;
* status;
* description.

Current game statuses include:

* `Draft`;
* `Open`;
* `Full`;
* `Cancelled`;
* `Completed`.

Current join policies include:

* `Open` — players can reserve available places directly;
* `ApprovalRequired` — a player requests a place and the organizer approves or rejects the request;
* `InviteOnly` — direct public joining is not available.

A place in a game represents participation capacity.

Player place booking should not be confused with future venue booking.

In the current phase, VolleyHub manages places inside the game, not the commercial reservation of the physical venue.

Future game restrictions may include:

* required player level;
* preferred position;
* player reliability;
* organizer blacklist;
* minimum attendance history;
* sex or mixed-game rules when relevant to a specific format;
* coach requirements.

## 8. Game Participation and Place Booking

A game participant represents a player's relationship with a specific game.

The participation lifecycle should support:

* requesting a place;
* receiving organizer approval when required;
* occupying game capacity;
* leaving before the game;
* being rejected;
* being removed by the organizer;
* joining a waitlist when the game is full.

Current participant states may include:

* `PendingApproval`;
* `Approved`;
* `Rejected`;
* `Left`;
* `Removed`.

Waitlisted players use `Waitlisted` and do not occupy confirmed capacity.

The exact state model may evolve as cancellation and waitlist behavior becomes more detailed.

The system should make it clear:

* whether the player currently has a place;
* whether approval is still required;
* whether the game is full;
* how many places are available;
* whether the player has cancelled;
* whether another player can take a released place.

### Waitlist

A full game accepts explicit waitlist entries before it starts. The initial implementation uses organizer-controlled FIFO promotion.

When a game becomes full:

1. additional players may join a waitlist;
2. an approved participant may cancel;
3. the released place becomes available;
4. the organizer promotes the next eligible player directly into the released place.

Promotion also grants approval for `ApprovalRequired` games. Waitlisted players have priority over new direct joins and pending approvals when capacity becomes available. There are no temporary offers or claim deadlines. See [Game waitlist](game-waitlist.md) for ordering, transitions, API contracts, and concurrency rules.

## 9. Coaches in Games

Coach participation is part of the near-term product direction, but the exact rules are still open.

A game may have:

* no coach;
* one coach;
* multiple coaches.

Possible requirements include:

* required coach count;
* optional coach count;
* coach price or payment expectation;
* coach assignment by organizer;
* coach application or self-booking;
* dedicated coach capacity.

The system should not force coaches into the normal player participation model until the real workflow is understood.

For now, coach participation should be treated as an explicit product design question rather than prematurely encoded into the domain.

## 10. Offline Payments

VolleyHub does not process money inside the application in the current phase.

This is intentional.

Players may pay the organizer using:

* cash;
* direct bank transfer;
* another method agreed between the organizer and player.

The organizer pays or settles with the venue outside VolleyHub.

The application may track the operational payment state without processing the payment itself.

Current payment states can include:

* `NotRequired`;
* `Pending`;
* `Paid`.

Possible future additions include:

* unpaid after deadline;
* refunded externally;
* partial payment;
* payment deadline.

In-app payments are deferred because they introduce significantly more complexity:

* payment providers;
* refunds;
* disputes;
* platform commissions;
* taxes;
* legal responsibility;
* chargebacks;
* support requirements.

Payment processing should only be added after the core game lifecycle is proven.

## 11. Attendance and Reliability

Attendance is a core part of VolleyHub.

The organizer needs to know whether players who reserved places actually came to the game.

After a completed game, the organizer can mark participation.

Current attendance states include:

* `NotMarked`;
* `Present`;
* `Absent`.

Only approved participants in completed games can be marked `Present` or `Absent`; `Absent` represents an explicitly recorded no-show. The organizer may correct either mark to the other or repeat it, but cannot reset it to `NotMarked`. Unmarked attendance is never inferred as absence.

Cancellation remains a separate participation fact: `Cancelled` with a saved `OnTime` or `Late` classification. The global threshold is 24 hours before the game starts. Cancelled participants cannot receive attendance marks, and organizer removal is separate from player cancellation.

Reliability summaries use completed games only and expose attended, no-show, on-time cancellation, late cancellation, and unmarked attendance counts. Cancelled or uncompleted games do not contribute. Attendance percentage uses only present and absent marks; cancellations and unmarked attendance do not change its denominator. Missing historical cancellation facts are not guessed. See [Participant cancellation, attendance, and reliability](game-participant-cancellation-lifecycle.md) for exact rules, corrections, compatibility, and API fields.

Reliability should be based on factual participation history.

The goal is not public shaming.

The goal is to help organizers protect limited game capacity from repeated unreliable behavior.

Possible later reliability features:

* organizer visibility into reliability before approval;
* reliability filters;
* temporary restrictions after repeated no-shows;
* player disputes for incorrect marks.

Reliability remains descriptive. Public ratings, arbitrary weighted scores, automatic penalties, and dispute workflows are outside the current implementation.

## 12. Organizer Operations

After the basic game flow works, the next product priority is reducing repetitive organizer work.

Important organizer improvements include:

* `My organized games`;
* upcoming games;
* completed games;
* cancelled games;
* pending join requests;
* participant management;
* available-place visibility;
* offline payment overview;
* attendance completion;
* recurring games;
* game templates;
* waitlist management;
* cancellation management;
* organizer history;
* organizer statistics.

Authenticated players can read their participation history and organized games through dedicated paged endpoints, including status/time filters and per-game operational summaries. See [Game history](game-history.md) for contracts and ordering rules.

Recurring games are especially important.

Many real amateur volleyball games repeat every week with almost identical settings.

Example:

```text
Monday
20:00-22:00
Sports Hall A
12 players
25 BYN
Intermediate
Approval required
```

An organizer should not need to recreate the same game manually every week.

Weekly recurring games now support finite schedules of 2–52 independent games generated immediately from an existing game's settings. Organizers can edit or cancel one occurrence, update future settings, or cancel the remaining series. The first version uses a fixed UTC weekly schedule and preserves each game's participation, offline payment, attendance, and history lifecycle. See [Recurring games](recurring-games.md) for schedule limits, scope rules, retries, concurrency, and API contracts.

### Game templates

An active player profile can save private, named game templates for games without a fixed schedule. A template stores `name` (required, up to 100 characters), `courtId`, optional positive `duration`, `maxPlayers`, `pricePerPlayer`, `requiredLevel`, `joinPolicy`, and optional `description`. Game settings follow the existing game validation rules. Names need not be unique. Duration uses the same TimeSpan JSON string as recurring games, for example `"02:00:00"`; null means no end time.

Using a template requires `startsAt`. It is normalized to UTC and follows ordinary game start validation. The end is calculated from the saved duration; dates outside the supported calendar range are rejected. Each use creates one independent, open `Game` with an empty participant list and no recurrence identity. Subsequent game edits use the ordinary game endpoints. Editing or deleting a template never changes existing games, participation, payments, attendance, or history. Templates do not store dates, participants, game status, or recurrence rules, and do not create a venue reservation.

All template endpoints require authentication and an active player profile. Ownership comes from that profile and cannot be supplied or transferred in a request. Other organizers receive `403`; a missing template, active profile, or court returns `404`. Invalid input returns `400`, and conflicting template writes return `409`, using ProblemDetails. The court must be active when saving or using a template. If it is later deleted, the template remains readable and can be repaired by selecting another active court, or deleted.

| Action | Endpoint | Success |
| --- | --- | --- |
| Create template | `POST /api/game-templates` | `201`, UUID body and `Location` |
| List own templates, ordered by name then ID | `GET /api/game-templates` | `200`, array of template details |
| Read own template | `GET /api/game-templates/{id}` | `200`, template details |
| Replace template settings | `PUT /api/game-templates/{id}` | `204` |
| Delete template | `DELETE /api/game-templates/{id}` | `204` |
| Create game with `{ "startsAt": "2027-01-04T17:00:00Z" }` | `POST /api/game-templates/{id}/games` | `201`, game UUID and game `Location` |

Create and update accept the saved fields listed above. Template details add `id`, `createdAt`, and nullable `updatedAt`. Deletion removes the template permanently; further access returns `404`. Games have no foreign key to templates. Apply the `AddGameTemplates` migration before deploying. Sharing, public template discovery, organization or venue ownership, and scheduling remain outside templates; recurring games stay a separate concept.

## 13. Training and Coach Direction

After organizer-led games are mature, VolleyHub should support training sessions more deeply.

A future `TrainingSession` may include:

* coach;
* court;
* start time;
* end time;
* player limit;
* player level;
* training focus;
* price;
* participant list;
* attendance.

Possible training focus examples:

* serving;
* reception;
* setting;
* attacking;
* blocking;
* defense;
* beginner fundamentals;
* physical preparation.

Coach profiles and training sessions should reuse common concepts from games where appropriate, but should not be forced into the same domain model if their rules are substantially different.

## 14. Player Engagement Vision

The long-term vision is not only to help users find one game.

VolleyHub should become useful enough that players return regularly.

Future engagement features may include:

* game history;
* attendance history;
* player statistics;
* preferred position;
* player level;
* goals;
* achievements;
* progress tracking;
* coach history;
* training history;
* personal profile;
* organizer statistics;
* badges;
* teams;
* social features later.

The core organizational value should come before social-network features.

## 15. Core Domains

### 15.1 Users / Auth

Responsible for:

* registration;
* login;
* identity;
* authentication;
* authorization.

The current product should avoid creating separate roles when normal domain ownership is enough.

For example, a player who creates a game becomes its organizer through the game relationship.

Possible explicit roles later may include:

* `Admin`;
* `VenueOwner`.

A dedicated coach role or profile should be introduced only when coach workflows require it.

### 15.2 Player Profiles

Responsible for player-related information.

Possible fields:

* display name;
* city;
* level;
* preferred position;
* attendance history;
* game history;
* reliability information;
* future statistics.

### 15.3 Courts

Responsible for physical places where games happen.

Possible fields:

* name;
* address;
* city;
* latitude;
* longitude;
* surface type;
* indoor/outdoor type;
* description;
* future venue id.

A court does not need an active venue account in the current product.

### 15.4 Games

Responsible for organizer-led volleyball games.

Possible fields:

* organizer id;
* court id;
* start time;
* end time;
* max players;
* price per player;
* required level;
* join policy;
* status;
* description;
* future coach requirements.

### 15.5 Game Participants

Responsible for player participation in games.

Possible fields:

* game id;
* player profile id;
* join status;
* attendance status;
* offline payment status;
* joined date;
* approved date;
* future cancellation information.

### 15.6 Coaches

This domain is planned but not fully designed yet.

Possible concepts:

* coach profile;
* game coach assignment;
* training sessions;
* coach availability;
* coach specialization;
* coaching history.

### 15.7 Organizer Profile

A separate organizer profile is optional and can be added when organizer-specific reputation and statistics justify it.

Possible fields:

* organized games count;
* completed games count;
* cancelled games count;
* total participants;
* average attendance;
* organizer rating;
* badges.

The existence of an organizer profile should not be required merely to create a game unless there is a clear product reason.

### 15.8 Venues — Later Stage

Venues remain part of the long-term VolleyHub vision.

They are not removed from the product.

They are intentionally deferred until the organizer-led game lifecycle is mature.

A future venue may represent:

* school;
* gym;
* sports hall;
* private sports center;
* another managed sports facility.

Possible venue fields:

* name;
* type;
* description;
* contacts;
* address;
* city;
* verification status;
* owner/admin user;
* rules.

### 15.9 Availability Slots — Later Stage

Future managed venues may publish available court time.

Possible fields:

* court id;
* start time;
* end time;
* price;
* status;
* notes.

Possible statuses:

* `Available`;
* `Reserved`;
* `Unavailable`;
* `Cancelled`.

### 15.10 Reservation Requests — Later Stage

`ReservationRequest` remains a planned future concept.

It is not part of the current organizer-led game flow.

It will be used when VolleyHub begins connecting organizers directly with venue owners.

A reservation request may contain:

* organizer id;
* venue id;
* court id;
* availability slot id;
* status;
* message;
* created date;
* confirmed date;
* rejected reason.

Possible statuses may include:

* `PendingVenueApproval`;
* `VenueApproved`;
* `OrganizerConfirmed`;
* `Rejected`;
* `Cancelled`;
* `Confirmed`.

A confirmed venue reservation may eventually create or connect to a game automatically.

## 16. Product Roadmap

### MVP 1A — Core Game Foundation

Status: completed foundation.

The first stage established the main technical and product path:

* registration and authentication;
* player profiles;
* courts;
* game creation;
* game discovery;
* game details;
* joining games;
* organizer approval;
* participant management;
* game completion;
* attendance marking;
* basic reliability history;
* browser end-to-end coverage.

This foundation proves the basic organizer-to-player flow.

### MVP 1B — Organizer-Led Game Operations

This is the current priority.

Goal:

> Make the complete lifecycle of a real organizer-led game reliable and convenient enough for regular use.

Focus areas:

* complete player place-booking lifecycle;
* better cancellation handling;
* late cancellation semantics;
* game capacity management;
* waitlist behavior;
* organizer participant management;
* offline payment tracking;
* organizer dashboard;
* upcoming and completed game history;
* recurring games;
* game templates;
* attendance completion;
* reliability improvements;
* minimal coach involvement where needed;
* operational edge cases around full, cancelled, completed, and changed games.

The organizer should be able to run a real recurring volleyball game primarily through VolleyHub.

### MVP 1C — Coaches and Training

After the organizer-led game lifecycle is mature:

* coach profiles;
* coach assignment;
* coach participation rules;
* training sessions;
* training discovery;
* training participation;
* training attendance;
* coach history.

### MVP 1D — Discovery and Map

Location is an important part of the product.

Future discovery improvements may include:

* map view;
* search by city;
* search by radius;
* nearby games;
* nearby courts;
* improved filters.

The data model should continue storing:

* city;
* address;
* latitude;
* longitude.

### MVP 2 — Managed Venues and Venue Owners

Managed venues are postponed, not removed.

This stage introduces direct interaction between VolleyHub and venue owners.

Possible features:

* venue owner accounts;
* venue profiles;
* managed courts;
* court/hall ownership;
* venue verification;
* availability schedule;
* prices;
* venue contacts;
* reservation requests;
* venue approval;
* organizer confirmation;
* availability blocking;
* automatic game creation after confirmed reservation.

Until this stage, organizers continue handling venue agreements and rental arrangements outside VolleyHub.

### Later Stages

Possible later product areas:

* in-app payments;
* platform commissions;
* refunds;
* notifications;
* chat;
* teams;
* tournaments;
* social feed;
* posts;
* comments;
* follows;
* advanced statistics;
* achievements;
* admin tools;
* dispute workflows.

## 17. Not Included in the Current Phase

The current organizer-led phase should not be expanded with unrelated complexity.

Not current priorities:

* direct venue-owner onboarding;
* managed venue availability;
* venue reservation requests;
* automatic venue booking;
* in-app payment processing;
* refunds;
* platform commissions;
* complex legal agreements;
* public social feed;
* comments;
* follows;
* teams;
* advanced achievements;
* chat;
* push notifications;
* full coach marketplace;
* advanced player analytics.

These ideas remain valid future directions.

They should not distract from making the game lifecycle reliable.

## 18. Technical Direction

VolleyHub backend follows Clean Architecture.

Expected structure:

* `VolleyHub.Domain` — domain entities and business rules;
* `VolleyHub.Application` — use cases, commands, queries, validation, and contracts;
* `VolleyHub.Infrastructure` — persistence, Entity Framework Core, PostgreSQL, and external integrations;
* `VolleyHub.Api` — HTTP API, controllers, authentication, and configuration.

Development should move through focused vertical slices.

A typical behavior change should include:

1. domain rules when required;
2. application use case;
3. infrastructure persistence when required;
4. API contract;
5. automated tests;
6. frontend integration where relevant;
7. end-to-end coverage for critical user flows.

Do not introduce future venue abstractions into current game code unless they are required by a current use case.

The code should remain extensible, but abstractions should be earned by real product requirements.

## 19. Product Principles

### Complete the core loop first

The primary loop is:

```text
Organizer creates game
        ↓
Players reserve places
        ↓
Organizer manages participants
        ↓
Game takes place
        ↓
Organizer marks attendance
        ↓
History and reliability are updated
```

This flow should be extremely solid before large new domains are introduced.

### Match real-life organization

VolleyHub should reflect how amateur volleyball actually works.

Usually:

* one person takes responsibility;
* the organizer already knows where the game will happen;
* players reserve limited places;
* payments often happen outside the platform;
* plans change;
* players cancel;
* some players do not show up;
* capacity matters;
* attendance matters.

The software should support this reality rather than forcing an artificial process.

### Make organizers valuable

Organizers create supply for the platform.

Without organizers, there are fewer games.

The product should save organizer time and help them run games reliably.

### Protect limited capacity

A place in a volleyball game has real value.

A player taking a place and not arriving can prevent another player from participating.

Cancellations, waitlists, attendance, and reliability are therefore core product concerns rather than secondary statistics.

### Keep payments simple until necessary

Tracking offline payment status is useful.

Processing money directly is a separate and much larger product problem.

Do not confuse the two.

### Add coaches based on real workflows

Coach participation should be designed after understanding how organizers and coaches actually interact.

Do not prematurely model a coach as simply another player if that does not reflect reality.

### Keep managed venues in the long-term architecture

Venue owners, availability, and reservation requests remain an important future direction.

They are deferred because the organizer-led game lifecycle should be proven first.

### Build trust slowly

Attendance, cancellations, reliability, payment status, and organizer reputation can affect real people.

The system should prefer factual history and transparent rules over public shaming or arbitrary scores.

## 20. Open Questions

The following questions should be answered gradually through product development and real usage.

### Current game lifecycle

1. When exactly does a player own a confirmed place?
2. What happens when an approved player cancels?
3. Should the explicit waitlist flow later offer automatic enrollment?
4. Should organizer-controlled waitlist promotion later support automation?
5. How late can a player cancel without affecting reliability?
6. How should a late cancellation differ from a no-show?
7. Can the organizer reopen a completed game if attendance was marked incorrectly?
8. Should organizers be able to ban players from their future games?
9. How visible should player reliability be?
10. How should incorrect attendance marks be disputed?

### Coaches

11. Does a coach consume one of the normal player places?
12. Should games have separate coach capacity?
13. Can a coach reserve a coach place themselves?
14. Can only the organizer assign coaches?
15. Can one game have multiple coaches?
16. When should a dedicated `CoachProfile` become mandatory?
17. Should training sessions reuse the `Game` model or use a separate domain entity?

### Organizer operations

18. Should recurring games later support local time-zone schedules and DST adjustments?
19. Should finite recurring schedules later support extension or incremental generation?
20. Should private game templates later support explicit sharing?
21. Which payment information does an organizer actually need?
22. Should players see their own offline payment status?
23. What organizer statistics are genuinely useful?

### Future managed venues

24. What legal flow is required for schools and official venues in Belarus?
25. Who is allowed to create and manage a venue?
26. How should venue verification work?
27. How should existing courts become linked to future venue profiles?
28. Should venue booking create a game automatically?
29. When should in-app venue payments be considered?

### Discovery

30. When should map functionality be added?
31. Which filters are most important for real players?
32. Should player positions be required or optional?
33. What player level model should VolleyHub use?
