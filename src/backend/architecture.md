# Backend Architecture

## Purpose
This document describes the maintained architecture for `src/backend`.
It should match the code we actively keep in shape today, not an aspirational future layout.

## Scope
The backend is a layered .NET solution with:
- GraphQL as the primary domain API
- REST reserved for auth and technical endpoints
- CQRS in `Application`
- Mapperly as the standard mapping tool
- HotChocolate projection-backed reads as the default path for simple list/detail queries

Primary feature vocabulary:
- `bin-locations`
- `depots`
- `drivers`
- `parcels`
- `routes`
- `users`
- `vehicles`
- `zones`

## Core Principles
- Keep transport thin. GraphQL resolvers and REST controllers delegate; they do not own business rules.
- Keep business behavior in `Application`.
- Keep GraphQL explicit. Domain entities may be returned from resolvers, but schema shape must always be controlled through explicit GraphQL types.
- Use Mapperly for object mapping. Avoid handwritten property-by-property mapping except where domain rules or EF tracking make it necessary.
- Prefer projection-backed GraphQL reads whenever the response shape maps cleanly to the entity graph.
- Keep projection-backed reads pure. Read services used with `[UseProjection]` return `IQueryable<TEntity>` and do not pre-project into DTOs.
- Organize by feature first, then by use case.
- Prefer one obvious place for each responsibility: commands, queries, reads, mappings, DTOs.
- Depend inward only.

## Solution Layout

```text
src/backend/
  src/
    LastMile.TMS.Api/
    LastMile.TMS.Application/
    LastMile.TMS.Domain/
    LastMile.TMS.Infrastructure/
    LastMile.TMS.Persistence/
  tests/
    LastMile.TMS.Api.Tests/
    LastMile.TMS.Application.Tests/
    LastMile.TMS.Architecture.Tests/
    LastMile.TMS.Domain.Tests/
    LastMile.TMS.Infrastructure.Tests/
```

## Project Responsibilities

### `LastMile.TMS.Api`
Transport and composition root.

Responsibilities:
- application startup and DI composition
- GraphQL schema, types, queries, and mutations
- REST controllers for auth and technical endpoints (e.g. driver photo upload; returned URL is stored via GraphQL)
- transport-level auth, error translation, CORS, Swagger, middleware

GraphQL feature structure:

```text
  LastMile.TMS.Api/GraphQL/
  Common/
    EntityObjectType.cs
    Query.cs
    Mutation.cs
    GraphQLErrorFilter.cs
    DomainExceptionErrorFilter.cs
  BinLocations/
    BinLocationInputs.cs
    BinLocationMappings.cs
    BinLocationMutations.cs
    BinLocationQueries.cs
    BinLocationTypes.cs
  Depots/
    DepotInputs.cs
    DepotMappings.cs
    DepotMutations.cs
    DepotQueries.cs
    DepotTypes.cs
  Drivers/
    DriverInputs.cs
    DriverMappings.cs
    DriverMutations.cs
    DriverQueries.cs
    DriverTypes.cs
  Parcels/
  Routes/
  Users/
  Vehicles/
  Zones/
```

Rules:
- Feature folders use a consistent file-role pattern:
  - `*Queries.cs`
  - `*Mutations.cs`
  - `*Inputs.cs`
  - `*Mappings.cs`
  - `*Types.cs`
- Omit files that a feature does not need.
- `*Inputs.cs` contains GraphQL input contracts only.
- `*Mappings.cs` contains Mapperly input mappers only.
- `*Types.cs` contains output types plus related filter/sort/nested GraphQL types.
- Resolvers do not talk to `AppDbContext` directly.
- Mutation resolvers use `ISender`.
- Query resolvers use either `ISender` or a read service for a given field, not both.

### `LastMile.TMS.Application`
Use-case layer and main home of backend behavior.

Responsibilities:
- commands, queries, handlers
- validators
- DTOs for command payloads and workflow/query responses
- feature read services
- application interfaces
- Mapperly mappings used by handlers
- MediatR pipeline behaviors

Canonical feature structure:

```text
  LastMile.TMS.Application/
  Common/
    Behaviors/
    Interfaces/
  BinLocations/
    Commands/
    Queries/
    DTOs/
    Mappings/
    Support/
  Depots/
    Commands/
      CreateDepot/
        CreateDepotCommand.cs
        CreateDepotCommandValidator.cs
        CreateDepotCommandHandler.cs
      UpdateDepot/
        UpdateDepotCommand.cs
        UpdateDepotCommandValidator.cs
        UpdateDepotCommandHandler.cs
      DeleteDepot/
        DeleteDepotCommand.cs
        DeleteDepotCommandHandler.cs
    DTOs/
    Mappings/
    Reads/
  Drivers/
    Commands/
      CreateDriver/
        CreateDriverCommand.cs
        CreateDriverCommandValidator.cs
        CreateDriverCommandHandler.cs
      UpdateDriver/
        UpdateDriverCommand.cs
        UpdateDriverCommandValidator.cs
        UpdateDriverCommandHandler.cs
      DeleteDriver/
        DeleteDriverCommand.cs
        DeleteDriverCommandHandler.cs
    Queries/
      GetDriver/
        GetDriverQuery.cs
        GetDriverQueryHandler.cs
    DTOs/
    Mappings/
    Reads/
  Users/
    Commands/
    Queries/
    DTOs/
    Mappings/
    Reads/
    Support/
  Parcels/
    Commands/
    DTOs/
    Mappings/
    Reads/
    Services/
```

Rules:
- One folder inside `Commands/` or `Queries/` equals one use case.
- Each use case uses separate files for request, validator, and handler.
- `Validator` exists only when needed.
- `DTOs/` contains request/result DTOs only, not projection read models.
- `Mappings/` contains Mapperly mapping classes and closely related mapping helpers.
- `Reads/` contains `I<Feature>ReadService` plus implementation when the feature uses projection-backed `IQueryable<TEntity>` reads.
- `Services/` is for feature-specific ports or helper services declared in `Application`.
- `Support/` is for feature-local rules and helpers that do not belong in DTOs, reads, or services.
- Features that use MediatR-backed reads as an exception to the default projection-backed path may omit `Reads/` and keep read orchestration under `Queries/`.
- Not every feature needs every folder. Keep the structure uniform, but do not add empty folders just for symmetry.

### `LastMile.TMS.Domain`
Framework-independent domain model.

Responsibilities:
- entities
- enums
- value-like domain concepts
- base abstractions that must stay infrastructure-agnostic

Does not contain:
- MediatR requests
- EF Core configuration
- GraphQL types
- Mapperly mappers

#### Parcel Status Graph

```text
Registered ──► ReceivedAtDepot ──► Sorted ──► Staged ──► Loaded ──► OutForDelivery ──► Delivered
    │                │                 │          │          │            │
    │                │                 │          │          │            └──► FailedDelivery ──► OutForDelivery
    │                │                 │          │          │
    │                └─────────────────┴──────────┴──────────┴──► Exception
    │                                                     (on hold / unsortable)
    └──► Exception
    └──► Cancelled
```

The `Exception` state is reachable from `Registered` and from every post-receive status through `Loaded`. It covers unsortable parcels (bad address, hold, configuration gaps) and is available from the warehouse sort station for both pre-receive and post-receive parcels.

### `LastMile.TMS.Infrastructure`
Adapters for external or runtime concerns.

Responsibilities:
- current user resolution
- email and background jobs
- file or storage maintenance jobs (e.g. orphan driver photo cleanup)
- geocoding and zone support
- auth server/validation wiring
- options binding for external services

### `LastMile.TMS.Persistence`
Database layer.

Responsibilities:
- `AppDbContext`
- EF Core entity configuration
- migrations
- identity and OpenIddict persistence wiring
- seeding

### `tests/*`
Layered test ownership.

- `LastMile.TMS.Api.Tests`: GraphQL and REST contract tests
- `LastMile.TMS.Application.Tests`: handlers, validators, read services
- `LastMile.TMS.Architecture.Tests`: dependency and convention rules
- `LastMile.TMS.Domain.Tests`: pure domain behavior
- `LastMile.TMS.Infrastructure.Tests`: adapter behavior

## Dependency Rules

Allowed dependency graph:

```text
Api -> Application
Api -> Infrastructure
Api -> Persistence

Infrastructure -> Application

Persistence -> Application
Persistence -> Domain

Application -> Domain

Domain -> (no project dependencies)
```

Practical rules:
- `Application` depends on abstractions such as `IAppDbContext`, not transport or persistence details.
- `Api` depends on `Application` contracts and read services, not feature-internal persistence code.
- `Infrastructure` and `Persistence` implement inward-facing abstractions.

## Request and Read Flow

### GraphQL Mutation Flow

```text
GraphQL mutation
-> resolver in Api
-> Input -> Dto via Api Mapperly mapper
-> ISender / MediatR
-> validation pipeline
-> command handler
-> Dto -> Entity via Application Mapperly mapper
-> domain rules / persistence
-> entity or workflow DTO result
-> GraphQL response
```

Notes:
- Resource mutations usually return domain entities so the same GraphQL object type is reused for queries and mutations.
- Workflow mutations may return DTOs when there is no shared resource graph. Examples: parcel registration result, password reset result.

### Preferred GraphQL Read Path

Default to projection-backed GraphQL reads whenever the response shape maps cleanly to the entity graph.

Preferred path:
- GraphQL resolver in `Api`
- `IReadService` in `Application` returning `IQueryable<TEntity>`
- HotChocolate middleware such as `[UseProjection]`, `[UseFiltering]`, `[UseSorting]`, and `[UsePaging]`
- explicit GraphQL object types controlling the schema

Use MediatR-backed query handlers only when projection-backed reads would add unnecessary complexity, such as:
- aggregate or dashboard-style payloads
- tree or layout responses
- bundled lookup data returned together with the main result
- workflow-specific reads
- reads with non-trivial orchestration, authorization, or conditional shaping

### Projection-Backed Query Flow

```text
GraphQL query
-> resolver in Api
-> IReadService.GetXxx() returning IQueryable<TEntity>
-> HotChocolate projection/filtering/sorting middleware
-> EF Core SQL translation
-> GraphQL response via explicit EntityObjectType<TEntity>
```

Use this for:
- simple list pages
- simple detail pages
- lookup/reference data
- fields where selection-set driven SQL shaping matters

Rules:
- Read services return `IQueryable<TEntity>`.
- No DTO projection in read services.
- No `.Select(new Dto(...))` in read services.
- No `.Include()` in read services that are meant to work with `[UseProjection]`.

### MediatR-Backed Query Flow

```text
GraphQL query
-> resolver in Api
-> ISender / MediatR
-> query handler
-> application orchestration and materialization
-> DTO or entity result
-> GraphQL response
```

Use this for:
- workflow-specific reads
- aggregate screens
- bundled lookup payloads
- reads with non-trivial authorization or orchestration
- cases where projection-backed GraphQL reads would not fit cleanly

## Command and Query Conventions

Standard command shapes:
- `CreateXCommand(CreateXDto Dto)`
- `UpdateXCommand(Guid Id, UpdateXDto Dto)`

Scalar-only commands are allowed for truly small actions such as:
- delete
- deactivate
- send password reset email

Queries:
- keep query request/handler pairs under `Queries/<UseCase>/`
- use handlers only when the read is aggregate-oriented, workflow-specific, or does not fit cleanly into the projection-backed path

## Mapperly Conventions

Mapperly is the standard mapper in this solution.

### Where mappers live

| Layer | Folder | Purpose |
|---|---|---|
| Api | `GraphQL/<Feature>/*Mappings.cs` | GraphQL `Input -> Application DTO` |
| Application | `<Feature>/Mappings/` | `DTO -> Entity`, `Entity -> DTO`, update mappings |

### Api mapper pattern
- `Input -> DTO` only
- no business logic
- no persistence access

### Application mapper pattern
- `DTO -> Entity` for create flows
- `DTO -> existing Entity` update methods for update flows
- `Entity -> DTO` for workflow/query responses when DTOs are still needed

### Manual mapping is still allowed only when justified
Permitted cases:
- domain-derived values
- normalization such as uppercasing country code
- generated identifiers/tracking numbers
- role assignment and status transitions
- overlap/capacity checks
- nested collection replacement logic
- EF tracked entity update constraints

Do not write manual property-by-property mapping just because a Mapperly mapping was not added yet.

## GraphQL Conventions

### Domain output types
- Domain entities exposed through GraphQL must use `EntityObjectType<TEntity>`.
- Output fields are always explicit.
- Never rely on implicit schema exposure for domain entities.

### Feature file roles
- `*Queries.cs`: root query extensions only
- `*Mutations.cs`: root mutation extensions only
- `*Inputs.cs`: input contracts only
- `*Mappings.cs`: Mapperly `Input -> DTO`
- `*Types.cs`: object types, filter types, sort types, nested supporting GraphQL types

### Filters and sorting
- Keep filter and sort types close to the resource type, usually in the same `*Types.cs`.
- Use explicit names in the GraphQL schema rather than leaking CLR type names.

### Read service boundary
- Projection-backed resolvers should return `IQueryable<TEntity>` from `Application`.
- Do not insert DTOs between read service and GraphQL projection middleware.

### DataLoader usage
Use batch loaders only for computed fields that cannot be satisfied by projection cleanly and would otherwise cause N+1 behavior.

## Current Read Strategy by Feature

Projection-backed today:
- `depots`
- `drivers`
- `parcels` list/options reads
- `routes`
- `users` list/detail reads
- `vehicles`
- `zones`

MediatR-backed today:
- `bin-locations`
- all mutations
- workflow queries such as user management lookups
- any future aggregate or orchestration-heavy read

The real rule is not domain label. Prefer projection-backed GraphQL reads by default, and fall back to MediatR query handlers only when the read needs orchestration or an aggregate-shaped payload.

## What Belongs Where

Put code in `Api` when:
- it defines GraphQL schema types, inputs, resolvers, transport auth, or transport error handling

Put code in `Application` when:
- it defines a use case, validator, read service, mapping, DTO, rule, or orchestration logic

Put code in `Domain` when:
- it is pure business model with no transport or infrastructure concerns

Put code in `Infrastructure` when:
- it talks to the outside world or runtime environment

Put code in `Persistence` when:
- it configures EF Core or owns database behavior

## Disallowed Patterns
- business logic inside controllers or GraphQL resolvers
- direct `AppDbContext` usage from `Api`
- projection read services returning DTOs or read models
- `Select(...)` DTO projection inside read services
- `.Include()` in projection-backed read services
- separate `Validators/` or `Handlers/` folders at feature root
- vague feature-local folders such as `Common/` when `Support/`, `DTOs/`, or `Services/` is more precise
- handwritten mapping instead of Mapperly without a concrete reason
- parallel REST and GraphQL domain endpoints without an active need

## Adding a New Feature or Resource

1. Add or update the domain entity in `Domain` if required.
2. Add persistence wiring and migrations in `Persistence`.
 3. In `Application`, create a feature folder with:
     - `Commands/<UseCase>/`
     - `Queries/<UseCase>/` if needed
     - `DTOs/`
     - `Mappings/`
     - `Reads/` when you choose projection-backed read services
     - optional `Services/` or `Support/`
  4. Use Mapperly for `DTO <-> Entity` work.
  5. Choose the read path:
     - projection-backed `IQueryable<TEntity>` by default
     - MediatR-backed query handler only when the read is aggregate-oriented, workflow-specific, or otherwise does not fit cleanly into the projection pipeline
  6. In `Api/GraphQL/<Feature>`, add:
     - `*Queries.cs`
     - `*Mutations.cs` if needed
   - `*Inputs.cs` if needed
   - `*Mappings.cs` if needed
   - `*Types.cs`
7. Register GraphQL types/extensions in DI.
8. Add tests in the owning backend test project.

## Verification

Recommended backend verification after structural or architectural changes:

```bash
dotnet build src/backend/src/LastMile.TMS.Application/LastMile.TMS.Application.csproj --no-restore
dotnet build src/backend/src/LastMile.TMS.Api/LastMile.TMS.Api.csproj --no-restore
dotnet test src/backend/tests/LastMile.TMS.Application.Tests/LastMile.TMS.Application.Tests.csproj --no-build
dotnet test src/backend/tests/LastMile.TMS.Api.Tests/LastMile.TMS.Api.Tests.csproj --no-build -- RunConfiguration.MaxCpuCount=1
```

Add broader test scopes when the change touches other backend layers.
