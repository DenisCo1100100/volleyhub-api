# VolleyHub Product Vision

## 1. Purpose

VolleyHub is a platform for finding people and places to play volleyball.

The main goal is to connect players who want to play volleyball with organizers and available volleyball courts, gyms, school halls, and other venues.

VolleyHub should help solve two problems at the same time:

1. Players often want to play but do not know where to find a court, a game, or enough people.
2. Many venues may have available sports spaces, but players do not know about them and venue owners do not know that there is demand.

The first version of VolleyHub should focus on making volleyball games easier to organize.

## 2. Initial Market

The initial market is Belarus.

The practical starting point is Minsk, but the product should not be technically limited to Minsk. Other cities in Belarus should also be able to use the platform if there are active organizers, players, and available courts.

## 3. Core Product Model

VolleyHub is a three-sided platform.

### 3.1 Venues

Venues are schools, gyms, sports halls, private courts, public courts, or other places where people can play volleyball.

A venue can provide:

* courts or halls;
* address and location;
* available time slots;
* price if the court is paid;
* rules;
* contacts;
* confirmation process.

For official venues such as schools or sports halls, VolleyHub should assume that access may require an agreement, approval, or other legal/administrative process outside the application.

At the start, VolleyHub should not try to fully handle legal agreements inside the product.

### 3.2 Organizers

Organizers are initiative-taking players who create games and gather other players.

An organizer can:

* choose a court or venue;
* create a game;
* define date and time;
* define player limit;
* define game level;
* manage participants;
* approve or reject join requests;
* mark attendance after the game;
* communicate with the venue if the court requires approval.

Organizers are important because many amateur volleyball games already happen because someone takes initiative and gathers people manually.

VolleyHub should make this process easier and eventually give organizers additional motivation.

Possible organizer benefits in the future:

* discounts from venues;
* organizer rating;
* profile badges;
* achievements;
* priority access to some slots;
* reduced platform fees if payments are added later;
* statistics for organized games.

### 3.3 Players

Players are regular users who want to join volleyball games.

A player can:

* browse games;
* join a game;
* request approval if needed;
* leave a game;
* see location, time, price, and participants;
* build game history;
* receive statistics and achievements in the future.

## 4. Payment Assumption

At the start, VolleyHub does not process payments inside the application.

This is intentional.

In-app payments would add complexity:

* payment providers;
* refunds;
* disputes;
* platform commissions;
* taxes;
* legal responsibility;
* support issues.

The first version should only display the price and payment expectations.

Players pay the organizer outside the application, for example:

* in cash;
* by direct transfer;
* using whatever method the organizer and players agree on.

The organizer and the venue handle payment, agreement, access, and legal details outside the application.

## 5. Court Types

VolleyHub should support two main types of courts.

### 5.1 Managed Courts

Managed courts are official or semi-official courts controlled by a venue.

Examples:

* school gym;
* sports hall;
* private gym;
* rented indoor court;
* venue-owned volleyball court.

Managed courts may require:

* venue approval;
* agreement between organizer and venue;
* payment;
* fixed schedule;
* contacts;
* rules.

For managed courts, VolleyHub uses `ReservationRequest`.

### 5.2 Community Courts

Community courts are free or informal places where people can play without venue approval.

Examples:

* outdoor court near a house;
* public court;
* park court;
* free city court;
* any place where players can gather without formal booking.

For community courts, no `ReservationRequest` is required.

A user can add a community court, create a game there, become the organizer, approve players if needed, and mark attendance after the game.

## 6. Main Flows

### 6.1 Managed Venue Flow

This flow is used when a court requires venue approval.

1. A venue creates a profile.
2. The venue adds one or more courts or halls.
3. The venue publishes available time slots.
4. An organizer chooses a suitable slot.
5. VolleyHub creates a `ReservationRequest`.
6. The organizer contacts the venue using provided contacts if needed.
7. The organizer and venue handle legal and payment details outside the application.
8. The venue approves or rejects the request.
9. The organizer confirms the arrangement.
10. The selected time slot becomes unavailable.
11. A public game is created.
12. Players can join the game.

### 6.2 Community Court Flow

This flow is used when no venue approval is needed.

1. A user adds a free/community court.
2. The user creates a game on that court.
3. The user becomes the organizer.
4. Players join or request to join depending on game settings.
5. The organizer manages participants.
6. After the game, the organizer marks who attended and who did not.

### 6.3 Future Venue-Led Flow

In the future, a venue may create a ready-made game itself.

Example:

A school or sports hall creates a volleyball game for a specific time slot. Players join and pay on-site.

This flow is not a first MVP priority because it may create additional legal and organizational questions:

* who is responsible for the game;
* who signs or accepts the agreement;
* who collects money;
* who manages participants;
* who handles no-shows and conflicts.

## 7. ReservationRequest

`ReservationRequest` represents a request from an organizer to use a managed court or venue time slot.

It is not a payment transaction and not a legal contract inside the application.

It is a product-level agreement flow that helps both sides confirm that a slot is no longer available and can be used for a game.

Possible statuses:

* `PendingVenueApproval`
* `VenueApproved`
* `OrganizerConfirmed`
* `Rejected`
* `Cancelled`
* `Confirmed`

When a `ReservationRequest` becomes `Confirmed`, VolleyHub can create a game automatically.

`ReservationRequest` should only be used for managed courts.

Community courts do not need reservation requests.

## 8. Games

A game is a volleyball event created by an organizer.

A game has:

* organizer;
* court;
* date and time;
* maximum number of players;
* price per player if payment is expected offline;
* level;
* join policy;
* participant list;
* status.

Possible game statuses:

* `Draft`
* `Open`
* `Full`
* `Cancelled`
* `Completed`

Possible join policies:

* `Open` — any player can join until the limit is reached.
* `ApprovalRequired` — organizer approves or rejects join requests.
* `InviteOnly` — only invited players can join.

Future join restrictions:

* player level;
* preferred position;
* reliability score;
* organizer blacklist;
* minimum attendance reputation.

## 9. Attendance and Reliability

VolleyHub should help organizers protect games from unreliable participants.

After a game, the organizer can mark each participant.

Possible attendance statuses:

* `Unknown`
* `Attended`
* `NoShow`
* `CancelledInAdvance`
* `LateCancel`

Possible offline payment statuses:

* `NotRequired`
* `Expected`
* `Paid`
* `NotPaid`

The goal is not public shaming.

The goal is to create a reliability system that helps organizers understand who actually comes to games and who repeatedly joins but does not show up.

Possible future reliability features:

* organizer can see player attendance history;
* organizer can block unreliable players from future games;
* repeated no-shows lower player reliability;
* players can see their own reliability status;
* players may dispute incorrect attendance marks;
* organizers can filter join requests by reliability.

## 10. Player Engagement Vision

The long-term vision is not only to help users find games.

VolleyHub should eventually become interesting for players to use regularly.

Future engagement features:

* player statistics;
* game history;
* attendance history;
* achievements;
* goals;
* progress tracking;
* player level;
* position tracking;
* personal profile;
* organizer profile;
* badges;
* community feed;
* posts;
* comments;
* follows;
* teams.

The long-term direction is partly similar to fitness community apps where users track activity, see progress, and interact with other people.

Players should return not only because they need to find a game, but also because they care about their volleyball progress and community identity.

## 11. Core Domains

### 11.1 Users / Auth

Responsible for registration, login, identity, and roles.

Possible roles:

* `Player`
* `Organizer`
* `VenueOwner`
* `Admin`

A user may have multiple roles.

For example, a player can also be an organizer.

### 11.2 Player Profiles

Responsible for player-related information.

Possible fields:

* display name;
* city;
* level;
* preferred position;
* reliability score;
* games joined;
* attendance history;
* achievements in the future.

### 11.3 Venues

Responsible for official or managed places.

Possible fields:

* name;
* type;
* description;
* contacts;
* address;
* city;
* verification status;
* owner/admin user;
* rules.

### 11.4 Courts

Responsible for specific places where games happen.

A court may belong to a venue, but it can also be a standalone community court.

Possible fields:

* name;
* venue id, nullable;
* court type;
* address;
* city;
* latitude;
* longitude;
* indoor/outdoor type;
* is free;
* reservation required;
* verification status.

Possible court types:

* `Managed`
* `Community`

Possible verification statuses:

* `Unverified`
* `Verified`

### 11.5 Availability Slots

Responsible for venue/court available time.

Possible fields:

* court id;
* start time;
* end time;
* price;
* status;
* notes.

Possible slot statuses:

* `Available`
* `Reserved`
* `Unavailable`
* `Cancelled`

### 11.6 Reservation Requests

Responsible for organizer-to-venue confirmation flow.

Possible fields:

* organizer id;
* venue id;
* court id;
* availability slot id;
* status;
* message;
* created date;
* confirmed date;
* rejected reason.

### 11.7 Games

Responsible for volleyball games.

Possible fields:

* organizer id;
* court id;
* reservation request id, nullable;
* start time;
* end time;
* max players;
* price per player;
* required level;
* join policy;
* status;
* description.

### 11.8 Game Participants

Responsible for players joining games.

Possible fields:

* game id;
* player id;
* join status;
* attendance status;
* offline payment status;
* joined date;
* approved date.

Possible join statuses:

* `PendingApproval`
* `Approved`
* `Rejected`
* `Joined`
* `Left`
* `Removed`

### 11.9 Organizer Profile

Responsible for organizer-specific statistics and reputation.

This may be implemented later, but the concept should exist from the beginning.

Possible fields:

* organized games count;
* completed games count;
* cancelled games count;
* average attendance;
* organizer rating;
* badges;
* venue discounts in the future.

## 12. MVP Scope

The first MVP should focus on the core game organization flow.

### MVP 1A — Community Games

This is the simplest path to real usage.

Required features:

* user registration/login;
* player profile;
* create community court;
* list courts;
* create game on community court;
* join game;
* organizer approval;
* participant list;
* mark attendance after game;
* basic reliability tracking.

No venue approval is required in this flow.

### MVP 1B — Managed Venues

This adds schools, gyms, and sports halls.

Required features:

* venue profile;
* court/hall creation;
* availability schedule;
* price display;
* venue contacts;
* reservation request;
* venue approval;
* organizer confirmation;
* automatic game creation after confirmed reservation;
* slot becomes unavailable after confirmation.

### MVP 1C — Map

A map is important because location is a key part of the product.

The exact MVP stage is open, but the data model should support maps from the beginning.

Courts and venues should store:

* city;
* address;
* latitude;
* longitude.

Even if the first UI uses a list, map support should be considered in the data model early.

## 13. Not Included in First MVP

The first MVP should not include:

* in-app payments;
* refunds;
* platform commissions;
* complex legal agreement handling;
* public social feed;
* comments;
* follows;
* teams;
* trainer marketplace;
* advanced statistics;
* complex achievements;
* chat;
* push notifications;
* mobile app if backend/API is the current focus.

These features can be added later after the core game organization flow works.

## 14. Future Ideas

Possible future features:

* player statistics;
* achievements;
* goals;
* organizer discounts;
* organizer reputation;
* player reliability score;
* team pages;
* game history;
* court ratings;
* venue ratings;
* social feed;
* posts;
* comments;
* follows;
* trainer profiles;
* training sessions;
* payments;
* online booking;
* map search;
* search by radius;
* notifications;
* chat;
* admin panel;
* dispute system for no-shows or unpaid participation.

## 15. Technical Direction

VolleyHub backend follows Clean Architecture.

Expected structure:

* `VolleyHub.Domain` — domain entities and business rules.
* `VolleyHub.Application` — use cases, commands, queries, and contracts.
* `VolleyHub.Infrastructure` — persistence, Entity Framework Core, PostgreSQL, and external integrations.
* `VolleyHub.Api` — HTTP API, endpoints, Swagger/OpenAPI.

Development should move by vertical slices.

A typical feature should include:

1. Domain model.
2. Application use case.
3. Infrastructure persistence.
4. API endpoint.
5. Swagger/manual testing.
6. Automated tests when needed.

## 16. Product Principles

### Start simple

Do not build payments, social network features, or complex statistics before the basic game flow works.

### Support real-life organization

The product should match how amateur volleyball games actually happen:

* someone organizes;
* people join;
* money is often handled offline;
* venues may require manual communication;
* attendance reliability matters.

### Do not hide real-world complexity

If schools or venues require agreements, VolleyHub should not pretend that booking is automatic.

The platform should help people coordinate, but legal and payment details can remain outside the application in early versions.

### Make organizers valuable

Organizers are one of the most important user types.

Without organizers, players have fewer games to join.

VolleyHub should make organizing easier and eventually reward reliable organizers.

### Build trust slowly

Reliability, no-shows, unpaid participation, and organizer reputation should be handled carefully.

The system should protect organizers and players without turning into public shaming.

## 17. Open Questions

These questions are not blockers for the first version, but should be clarified later:

1. What legal flow is required for schools and official venues in Belarus?
2. Can a school or venue create games directly, or should every game have a separate organizer?
3. How should disputes around no-shows or unpaid participation be handled?
4. Should reliability be visible to everyone or only to organizers?
5. When should map functionality be added?
6. What player levels should be supported?
7. Should positions be required for games or optional?
8. Should organizers be able to ban players from their games?
9. Should community courts be moderated before becoming public?
10. Should venue verification be manual by admin?
