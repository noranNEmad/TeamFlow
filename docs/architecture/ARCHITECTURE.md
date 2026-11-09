# TeamFlow Architecture & Domain Design Document

## 1. System Overview & Core Domain
**TeamFlow** is an enterprise-grade Project Management SaaS platform.
The domain represents workspaces, teams, projects, and work items with strict multi-tenant boundaries.

### Core Domain Entities
1. **User**: Represents system users with global authentication details (Email, PasswordHash, FullName, AvatarUrl, IsActive).
2. **Organization**: Represents the tenant boundary. Every team, project, and task lives under an Organization.
3. **OrganizationMember**: Junction table handling tenant-level roles (`Owner`, `Admin`, `Member`).
4. **Team**: Functional groups within an organization (e.g., Engineering, Marketing).
5. **TeamMember**: Maps users to teams with team-level roles (`Member`, `Lead`).
6. **Project**: Managed within an organization, optionally assigned to a specific team. Identified by a unique `Key` (e.g. `TF`, `API`).
7. **TaskItem**: Granular work item belonging to a project, having status (`Backlog`, `Todo`, `InProgress`, `InReview`, `Done`), priority (`Low`, `Medium`, `High`, `Urgent`), creator, assignee, and due date.
8. **Comment**: Granular feedback and discussion thread attached to a `TaskItem`, authored by a `User`.

---

## 2. Database Schema & Fluent API Configurations (PostgreSQL)

| Table | Primary Key | Foreign Keys & Relations | Key Indexes & Constraints |
|---|---|---|---|
| `users` | `Id` (UUID) | None | Unique `Email`, `FullName` |
| `organizations` | `Id` (UUID) | None | Unique `Slug`, `Name` |
| `organization_members` | `(OrganizationId, UserId)` (Composite) | `OrganizationId` (Cascade), `UserId` (Restrict) | Composite PK, `Role` (Enum int) |
| `teams` | `Id` (UUID) | `OrganizationId` (Cascade) | Index on `(OrganizationId, Name)` |
| `team_members` | `(TeamId, UserId)` (Composite) | `TeamId` (Cascade), `UserId` (Restrict) | Composite PK, `Role` (Enum int) |
| `projects` | `Id` (UUID) | `OrganizationId` (Cascade), `TeamId` (SetNull) | Unique Index on `(OrganizationId, Key)` |
| `tasks` | `Id` (UUID) | `ProjectId` (Cascade), `CreatorId` (Restrict), `AssigneeId` (SetNull) | Index on `(ProjectId, Status)`, Index on `AssigneeId` |
| `comments` | `Id` (UUID) | `TaskItemId` (Cascade), `AuthorId` (Restrict) | Index on `TaskItemId` |

### Running the Database (Docker Compose)
To start the local PostgreSQL instance:
```bash
docker compose up -d
```
Connection String configured in `appsettings.json`:
`Host=localhost;Port=5432;Database=TeamFlowDb;Username=postgres;Password=postgres`

---

## 2. Clean Architecture Layering & Responsibilities

```
┌────────────────────────────────────────────────────────┐
│                      TeamFlow.API                      │
│   (Controllers, Minimal APIs, Middleware, Program.cs)   │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│                  TeamFlow.Application                  │
│       (DTOs, Use Cases, Interfaces, Validators)        │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│                    TeamFlow.Domain                     │
│    (Entities, Enums, Value Objects, Domain Events)     │
│                 * ZERO External Dependencies *         │
└───────────────────────────▲────────────────────────────┘
                            │
┌───────────────────────────┴────────────────────────────┐
│                 TeamFlow.Infrastructure                │
│    (PostgreSQL DbContext, EF Core, Repositories, Redis)│
└────────────────────────────────────────────────────────┘
```

### Dependency Rules:
- **`TeamFlow.Domain`**: Pure C# objects (POCOs). No EntityFrameworkCore packages, no ASP.NET dependencies.
- **`TeamFlow.Application`**: Business rules, contracts, and interfaces. Depends only on `Domain`.
- **`TeamFlow.Infrastructure`**: Implementation of data persistence, external services, and database contexts. Depends on `Application` and `Domain`.
- **`TeamFlow.API`**: Presentation layer and entry point. Injects dependencies into DI container.

---

## 3. Multi-Tenancy Strategy
- In **Level 1 (v1.0)**, Multi-tenancy is enforced through the `OrganizationId` foreign key and tenant scoping.
- In **Level 2 (v2.0)**, we will layer EF Core Global Query Filters (`HasQueryFilter(e => e.OrganizationId == _currentTenantService.TenantId)`) and middleware to guarantee strict cross-tenant isolation automatically.

