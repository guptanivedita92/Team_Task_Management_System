# Team Task Management System

A role-based full-stack task management application built for the "Team Task Management System –
Role-Based Full-Stack Application Assessment". Backend: ASP.NET Core Web API + EF Core + SQL Server.
Frontend: React + Axios.

---

## 1. Project Overview

Organizations can manage teams, assign tasks, track task progress, collaborate through comments, and
get notified of important task events — all governed by three roles: **Admin**, **Manager**, **User**.
Every authorization rule is enforced **server-side**; the frontend hides irrelevant UI for a better
experience, but the API independently rejects anything a role isn't allowed to do.

## 2. Features

- JWT authentication (register/login), password hashing via ASP.NET Core's `PasswordHasher<T>`
- Role-based authorization enforced in the service layer (not just `[Authorize]` attributes)
- User management, team management (create/update/delete, add/remove members)
- Task CRUD with status (`ToDo`/`InProgress`/`Done`), priority, deadlines, filtering, and search
- Comments on tasks, scoped to whoever can already view the task
- In-app notifications on task assignment and status change, with mark-read / mark-all-read
- Dashboard with status counts, tasks-by-user, tasks-by-priority, and upcoming deadlines
- Swagger UI for exploring/testing the API
- Docker Compose setup (backend + frontend + SQL Server)
- xUnit test suite covering auth, authorization/IDOR rules, and notification triggers

## 3. Roles & Permissions

| Action | Admin | Manager | User |
|---|---|---|---|
| Manage all users/roles | ✅ | — | — |
| Create/manage teams | ✅ | own team's members only | — |
| Create/assign tasks | ✅ (any team) | ✅ (own team only) | — |
| View tasks | all | own team's tasks | only tasks assigned to them |
| Update task status | any task | own team's tasks | only their own assigned task |
| Delete tasks | any | own team's tasks | — |
| Comment on a task | any task | own team's tasks | only tasks assigned to them |
| View dashboard | org-wide | own team | own tasks only |

A user cannot widen this by editing request payloads or task IDs directly — every service method
re-checks the caller's role/team/ownership against the database, not just what the UI shows them.

## 4. Technology Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8 Web API, C# |
| ORM | Entity Framework Core 8 (Code First) |
| Database | SQL Server |
| Auth | JWT Bearer, `PasswordHasher<T>` |
| Frontend | React 18, React Router, Axios, Vite |
| Docs | Swagger / OpenAPI |
| Tests | xUnit, EF Core InMemory provider |
| DevOps | Docker Compose, GitHub Actions |

## 5. Architecture

```
Backend/TaskManagement.Api/
    Controllers/     thin HTTP endpoints only
    Services/        business logic + authorization rules
    Interfaces/      service contracts, ApiException
    Models/          EF Core entities + enums
    DTOs/            request/response shapes (entities never returned directly)
    Data/            AppDbContext, DbSeeder
    Middleware/      global exception handler (consistent error shape, no leaked internals)
    Helpers/         JWT generation, claims extensions
Backend/TaskManagement.Tests/
    service-layer unit tests (in-memory DB, no real SQL Server needed to run them)
frontend/src/
    pages/, components/, layouts/, context/ (auth), services/ (axios calls)
```

Controllers stay thin: they extract the caller's identity from JWT claims and delegate everything
else to a service. Every service method that needs to check "can this caller do this?" takes the
caller's `userId`, `role`, and `teamId` explicitly and checks them against the resource being
touched — this is what prevents IDOR (e.g. a User changing a task's ID in the URL to view/edit
someone else's task).

## 6. Project Structure

```
team-task-management-system/
    backend/
        TaskManagement.Api/
        TaskManagement.Tests/
        TeamTaskManagement.sln
    frontend/
    .github/workflows/ci.yml
    docker-compose.yml
    .env.example
    README.md
```

## 7. Database Design

- `User` —(TeamId, nullable)→ `Team`
- `Team` —(ManagerId, nullable)→ `User`
- `TaskItem` → `AssignedUser` (User, nullable), `CreatedBy` (User, required), `Team` (required)
- `Comment` → `TaskItem`, `User`
- `Notification` → `User`

Unique indexes on `User.Email` and `Team.Name`. Enums (`Role`, `TaskStatus`, `Priority`,
`NotificationType`) are stored as strings in the database and serialized as strings over the API
(not raw numbers) for readability.

## 8. API Overview

Base path: `/api`. Key resources: `auth`, `users`, `teams`, `tasks`, `tasks/{id}/comments`,
`notifications`, `dashboard`. Full request/response schemas are in Swagger UI (see below) — that's
the source of truth over this document for exact field names.

Standard status codes are used throughout: `200/201/204` for success, `400` for validation errors,
`401` for missing/invalid auth, `403` for role/ownership violations, `404` for missing resources,
`409` for conflicts (duplicate email/team name, deleting a team that still has tasks).

## 9. Authentication

- `POST /api/auth/register` — creates an account, always with the `User` role (role escalation only
  happens through the admin-only `PATCH /api/users/{id}/role` endpoint, never through registration).
- `POST /api/auth/login` — returns a JWT (`Authorization: Bearer <token>`), default 60-minute expiry.
- Passwords are hashed with ASP.NET Core's `PasswordHasher<T>` (PBKDF2-based) — never stored or
  returned in plaintext.

## 10. Local Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- SQL Server (local install, or via Docker — see section 15)

### Clone & configure secrets
```bash
git clone <your-repo-url>
cd team-task-management-system/backend/TaskManagement.Api

# JWT signing key and DB connection string are NEVER committed - set them via user-secrets:
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "a-random-string-at-least-32-characters-long"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
```
(Use a SQL auth connection string instead of `Trusted_Connection` if that's how your SQL Server is configured.)

## 11. SQL Server Configuration

Any SQL Server 2019+ instance works (local install, Docker, or Azure SQL). Update the connection
string above to match. The app runs EF Core migrations automatically on startup (see below), so
you just need an empty, reachable database.

## 12. EF Core Migrations

**This repository does not include pre-generated migration files.** They were deliberately left for
you to generate locally, where a real .NET SDK can produce and verify them — hand-authoring EF Core's
generated snapshot files without being able to compile them is exactly the kind of thing that looks
right but silently breaks, so instead:

```bash
cd backend/TaskManagement.Api
dotnet tool install --global dotnet-ef   # if you don't already have it
dotnet ef migrations add InitialCreate
```

This creates `Migrations/`. From then on, `dotnet run` (see below) applies migrations automatically
on startup via `db.Database.MigrateAsync()`.

## 13. Backend Startup

```bash
cd backend/TaskManagement.Api
dotnet restore
dotnet ef migrations add InitialCreate   # first time only, see section 12
dotnet run
```

The API listens on the URL printed in the console (typically `https://localhost:7xxx` or
`http://localhost:5000`). Migrations apply and the database is seeded automatically on first run.

**Seeded dev/demo accounts** (password for all: `Passw0rd!123` — dev-only, never use in production):

| Role | Email |
|---|---|
| Admin | `admin@taskmanagement.dev` |
| Manager | `manager@taskmanagement.dev` |
| User | `user@taskmanagement.dev` |

## 14. Frontend Startup

```bash
cd frontend
cp .env.example .env.local   # adjust VITE_API_BASE_URL if your backend runs elsewhere
npm install
npm run dev
```

Opens at `http://localhost:5173`.

## 15. Docker Instructions

```bash
cp .env.example .env   # fill in SQL_SA_PASSWORD and JWT_KEY - see .env.example
docker compose up --build
```

This starts SQL Server, the backend (port 5000), and the frontend (port 5173). The backend waits for
SQL Server's health check before starting and applies migrations on boot.

**Known limitation:** the `mcr.microsoft.com/mssql/server` image is x86_64-only. On Apple Silicon
(M1/M2/M3) Macs it runs under emulation, which is slower to start and, on some hosts, unstable. If
that's your environment, running SQL Server natively (or via Azure SQL Edge's ARM-compatible image)
and pointing the backend at it directly is more reliable than the bundled compose service.

**Also note:** the frontend Dockerfile runs `npm ci`... actually `npm install` at build time — see
section 19's note on `package-lock.json`, since a missing lockfile could make dependency versions
drift slightly between builds until one is committed.

## 16. Testing Instructions

```bash
cd backend
dotnet test
```

Tests use EF Core's InMemory provider, so **no SQL Server connection is required to run them.**
Coverage focuses on what the assignment calls out as priorities: registration/login/password
hashing, role-based authorization (including IDOR — e.g. a User cannot view or modify a task they
aren't assigned to, even by guessing its ID), task assignment and status-change notification
triggers, team member-management scoping, and comment access control.

## 17. Swagger Instructions

With the backend running in Development mode, open `/swagger` (e.g. `http://localhost:5000/swagger`).
Click **Authorize**, paste `Bearer <your JWT from /api/auth/login>`, and you can exercise every
endpoint from the browser. (Minor cosmetic note: Swagger's schema view shows enum fields as
integers rather than the string names the API actually sends/accepts — the API itself uses strings
like `"Admin"` / `"InProgress"`, as shown in this README and used by the frontend.)

## 18. Sample Development Credentials

See section 13. **These are development/demo credentials only** — rotate or remove them before any
real deployment; `DbSeeder` only seeds if the `Users` table is empty, so deleting these accounts in
a real environment prevents them from reappearing.

## 19. Environment Variables

| Variable | Where | Purpose |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | backend | SQL Server connection string |
| `Jwt__Key` | backend | JWT signing secret, 32+ characters, never committed |
| `Jwt__Issuer` / `Jwt__Audience` | backend | JWT validation values (safe to keep in `appsettings.json`) |
| `Cors__AllowedOrigins__0` | backend | Frontend origin allowed to call the API |
| `VITE_API_BASE_URL` | frontend | Base URL the frontend calls |
| `SQL_SA_PASSWORD` | docker-compose | SQL Server `sa` password |
| `JWT_KEY` | docker-compose | same as `Jwt__Key` above, for the containerized backend |

None of these have real values committed anywhere in this repo — `.env`, `.env.local`, and
`appsettings.*.json` with real secrets are all `.gitignore`d; only `.env.example` (placeholders) is
committed.

**Note on the frontend lockfile:** this environment couldn't reach npm's registry to actually run
`npm install`, so `frontend/package-lock.json` isn't included. Run `npm install` once locally (with
network access) and commit the generated lockfile — after that, both the Dockerfile and CI workflow
can safely switch from `npm install` to `npm ci` for fully reproducible installs.

## 20. CI/CD Information

`.github/workflows/ci.yml` runs on every push/PR to `main`: restores, builds, and tests the backend
(`dotnet restore/build/test`), and installs + builds the frontend (`npm install && npm run build`).
It does not deploy anywhere — add a deploy job (Render/Railway/Netlify/Vercel, per the assignment's
bonus suggestions) if you want that.

## 21. Known Limitations

- **No pre-generated EF Core migrations** — see section 12. Generate them once locally.
- **No frontend `package-lock.json`** — see section 19's note; generate it once locally with `npm install`.
- **This project was built and reviewed without a working .NET SDK or internet access in the
  authoring environment.** Every file was hand-written and manually cross-checked for namespace/type
  consistency (including a specific check for `TaskStatus` colliding with
  `System.Threading.Tasks.TaskStatus`), but it has **not been compiled or executed** by the author.
  Please run `dotnet build`, `dotnet test`, and `npm run build` yourself as the real verification step
  before treating this as final — and please open an issue/report back anything that doesn't build,
  since it couldn't be caught here.
- JWT claims (including `teamId`) are set at login time; if an admin moves a user to a different team
  mid-session, that user's existing token still reflects their old team until they log in again.
  A production system would likely re-check team membership from the DB on sensitive operations
  rather than trusting the claim, or use shorter-lived tokens with refresh.
- Search (`?search=`) uses a simple `Contains` filter, not full-text search — fine at assessment
  scale, not meant to scale to a large dataset.
- No email notifications — in-app/mock notifications only, per the assignment's explicit allowance.
- No rate limiting on `/api/auth/login` — would be a reasonable production hardening addition.
- Swagger's generated schema shows enums as integers even though the API's actual JSON uses strings
  (see section 17) — a cosmetic documentation gap, not a functional one.

## 22. Future Improvements

- Refresh tokens instead of a single 60-minute access token.
- Real email notifications (e.g. via SendGrid) as an alternative to in-app-only.
- Pagination on `/api/tasks` and `/api/users` for large datasets.
- Rate limiting on auth endpoints.
- Deployment pipeline (Render/Railway for backend + DB, Netlify/Vercel for frontend).
- Audit log of who changed what on a task (beyond just `UpdatedAt`).
