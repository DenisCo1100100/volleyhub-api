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

- `VolleyHub.Domain` - domain entities and business rules
- `VolleyHub.Application` — application use cases and contracts
- `VolleyHub.Infrastructure` — persistence and external integrations
- `VolleyHub.Api` — HTTP API

## Local Infrastructure

The project uses PostgreSQL for local development.

### Start PostgreSQL

```bash
docker compose up -d