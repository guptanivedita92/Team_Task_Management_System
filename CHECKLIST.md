# Final Requirement Checklist

Verified against the original assignment PDF and the detailed instructions document. "How tested"
marked **(reasoned, not run)** means I traced the code path manually since I could not compile or
execute this project in the authoring environment (no .NET SDK, no internet access) — see README
section 21. Everything else marked "unit test" has an actual xUnit test in `TaskManagement.Tests`.

| Requirement | Implemented? | Where | How tested |
|---|---|---|---|
| JWT-based login, role-based access | ✅ | `AuthController`, `JwtTokenGenerator`, `[Authorize(Roles=...)]` throughout | unit test (`AuthServiceTests`) |
| Secure password hashing | ✅ | `AuthService` via `IPasswordHasher<User>` | unit test: hash ≠ plaintext |
| Token expiration | ✅ | `JwtTokenGenerator` (`ExpiryMinutes`, `ValidateLifetime=true`) | reasoned, not run |
| User registration & login | ✅ | `AuthController`, `AuthService` | unit test |
| Backend-enforced authorization (not just UI) | ✅ | Every service method takes `role`/`teamId`/`userId` and checks them (`TaskService`, `TeamService`, `UserService`, `CommentService`) | unit test (IDOR cases in `TaskServiceTests`, `UserServiceTests`) |
| Task CRUD + status tracking | ✅ | `TasksController`/`TaskService` | unit test |
| Statuses: To Do / In Progress / Done | ✅ | `Models.TaskStatus` enum | reasoned, not run |
| Team management incl. manager-assigns-users | ✅ | `TeamsController`/`TeamService` (`AddMemberAsync` allows Manager for own team) | unit test |
| Comments on tasks | ✅ | `CommentsController`/`CommentService`, access mirrors task visibility | unit test |
| Notifications on assignment + status update | ✅ | `TaskService` calls `INotificationService.NotifyAsync` on both events | unit test |
| Dashboard: status overview, by-user, by-priority, upcoming deadlines | ✅ | `DashboardController`/`DashboardService` | reasoned, not run |
| Filtering by deadline/status/priority | ✅ | `TaskFilterDto` applied in both `TaskService` and `DashboardService` | reasoned, not run |
| .NET backend, EF Core, SQL Server | ✅ | throughout; `AppDbContext` uses `UseSqlServer` | reasoned, not run |
| React + Axios frontend | ✅ | `frontend/src` | reasoned, not run |
| Backend-only auth enforcement (no client-only role hiding) | ✅ | `ProtectedRoute.jsx` is explicitly commented as UX-only; every real check is server-side | reasoned, not run |
| DTOs used, entities never exposed directly | ✅ | all controllers return `*ResponseDto` types; `PasswordHash` never in a DTO | grep-verified (see security review below) |
| Swagger/OpenAPI | ✅ | `Program.cs` `AddSwaggerGen` with JWT bearer scheme | reasoned, not run |
| Docker multi-container (backend/frontend/DB) | ✅ | `docker-compose.yml`, both `Dockerfile`s | reasoned, not run — see README §15 limitation on SQL Server image architecture |
| Unit/integration tests | ✅ | `TaskManagement.Tests` (33 tests across Auth/Task/Team/User/Comment/Notification services) | this is the test suite itself |
| GitHub Actions CI | ✅ | `.github/workflows/ci.yml` (restore/build/test backend, install/build frontend) | reasoned, not run — see README §19 lockfile note |
| README with full setup instructions | ✅ | `README.md` | — |
| No hard-coded secrets | ✅ | `appsettings.json` has empty placeholders with comments; `.env.example` only; `.gitignore` excludes real `.env`/secrets | grep-verified, see below |

## Security review (Step 16 of the process) — findings

Performed as a source-level grep/read audit (see conversation for the exact commands run):

- **Plain-text passwords:** none found — all paths hash via `IPasswordHasher<User>`.
- **Hard-coded JWT secrets / DB credentials:** none found — both come from configuration only, and
  `JwtTokenGenerator` throws on startup if the key is missing or under 32 characters.
- **Sensitive fields in API responses:** `PasswordHash` is only referenced in `Program.cs` (DI setup),
  `Models/User.cs` (the entity itself), `AuthService` (hash/verify), and `DbSeeder` (seeding) — never
  in a DTO or controller response.
- **Missing authorization:** every controller except `AuthController` (intentionally public for
  register/login) carries `[Authorize]`, plus role/ownership checks in the service layer.
- **IDOR:** explicitly tested — a User cannot view, comment on, or change the status of a task not
  assigned to them, even with a guessed/incremented ID; a Manager cannot touch another team's tasks
  or members.
- **CORS:** origin allowlist is configuration-driven, not wildcarded.
- **Secrets in git:** `.gitignore` excludes `.env`/`*.env` (keeping `.env.example`), `bin/`, `obj/`,
  `node_modules/`. No secrets are committed anywhere in this repo.

## What I could not verify myself

I do not have a working .NET SDK or internet access in this environment (see README §21). I have
not run `dotnet build`, `dotnet test`, `dotnet ef migrations add`, `npm install`, or `npm run build`
against this code. Everything above reflects careful manual tracing of the code (including specific
checks like the `TaskStatus` vs. `System.Threading.Tasks.TaskStatus` naming collision, cross-checking
every DI registration in `Program.cs` against an actual interface + implementation, and confirming
every service's imports resolve), not an actual green build. Please treat your own `dotnet build` /
`dotnet test` / `npm run build` as the real final gate, and let me know what surfaces if anything does.
