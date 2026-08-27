# VolleyHub API

Backend API for VolleyHub — a platform for finding and creating volleyball games.

## Tech Stack

- ASP.NET Core
- PostgreSQL
- Entity Framework Core
- Clean Architecture
- Docker
- GitHub Actions

## Project Structure

- `VolleyHub.Domain` — domain entities and business rules
- `VolleyHub.Application` — application use cases and contracts
- `VolleyHub.Infrastructure` — persistence and external integrations
- `VolleyHub.Api` — HTTP API

## Local Infrastructure

The project uses PostgreSQL for local development.

### Start PostgreSQL

```bash
docker compose up -d
```

## Authentication

VolleyHub uses short-lived JWT access tokens and rotating refresh tokens.

Access tokens:

- are returned in the authentication response;
- expire after 15 minutes by default;
- are intended to be stored in frontend memory;
- are sent using the `Authorization: Bearer <token>` header.

Refresh tokens:

- expire after 30 days by default;
- are persisted in the database only as SHA-256 hashes;
- rotate after every successful refresh;
- are revoked after logout;
- detect reuse of previously rotated tokens;
- must not be stored in browser local storage.

### Browser refresh token cookie

The raw refresh token is delivered using the `volleyhub.refreshToken` cookie.

For local development the cookie is configured as:

```text
HttpOnly
Secure
SameSite=None
Path=/api/auth
```

Because the cookie is `Secure`, the API should be accessed over HTTPS during browser development.

The default local frontend origin is:

```text
http://localhost:5173
```

Cross-origin browser requests that need the refresh cookie must include credentials.

Example:

```javascript
await fetch("https://localhost:<api-port>/api/auth/refresh", {
    method: "POST",
    credentials: "include"
});
```

The API CORS policy allows credentials for explicitly configured frontend origins.

### Authentication endpoints

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
```

`register`, `login`, and `refresh` return the access token in the response body and set the refresh token cookie.

`logout` revokes the current refresh token and deletes the refresh token cookie.

The frontend should keep the access token in memory and call `/api/auth/refresh` when a new access token is required.
