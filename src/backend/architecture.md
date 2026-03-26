# Backend Architecture

## Purpose
This document defines the target architecture for `src/backend`.
It describes the intended steady state of the backend, even if some current code still needs migration to fully match it.

## Scope
The backend solution is a layered .NET application with a shared application core and a GraphQL-first domain API.

Primary backend domains:
- `depots`
- `drivers`
- `parcels`
- `routes`
- `users`
- `vehicles`
- `zones`

Transport policy:
- GraphQL is the primary API for domain reads and writes.
- REST is retained only for auth and system-oriented endpoints such as `/connect/*`, test support, and similar technical endpoints.

## Core Principles
- Keep transport thin. Controllers and GraphQL resolvers should delegate instead of owning business logic.
- Keep GraphQL primary for domain behavior. Do not maintain parallel REST and GraphQL business endpoints without an active client need.
- Use CQRS intentionally. Commands and complex reads go through MediatR handlers.
- Use HotChocolate data middleware intentionally. Simple read-only list and detail queries may use projection-backed read services.
- Do not expose domain entities directly through GraphQL. Use DTOs for handler-backed flows and dedicated read models for projection-backed flows.
- Keep business workflows in `Application`. Validation, orchestration, and use-case rules belong there.
- Depend inward only. Outer layers may reference inner layers; inner layers must not reference transport concerns.
- Test architecture as code. Dependency and resolver rules are enforced with dedicated tests.

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
  Dockerfile
  LastMile.TMS.slnx
```

## Project Responsibilities

### `LastMile.TMS.Api`
This is the transport and composition root.

Responsibilities:
- application startup and middleware pipeline
- dependency injection composition
- GraphQL schema, types, queries, and mutations
- REST controllers for auth and system endpoints
- HTTP auth and authorization defaults
- CORS, Swagger, Problem Details, and request logging
- transport-specific error handling

Canonical structure:

```text
LastMile.TMS.Api/
  Configuration/
  Controllers/
  Diagnostics/
  GraphQL/
  Program.cs
```

Rules:
- GraphQL domain endpoints belong here as feature-scoped type extensions.
- REST controllers should exist only for auth and system endpoints.
- resolvers and controllers must not contain business rules
- resolvers and controllers must not reach into `AppDbContext` directly
- mutation resolvers depend on `ISender` only
- query resolvers depend on either `ISender` or a read service, never both in the same field

### `LastMile.TMS.Application`
This is the use-case layer and the main home of backend behavior.

Responsibilities:
- commands, queries, and handlers
- DTOs returned by MediatR-backed use cases
- validators
- MediatR pipeline behaviors
- application interfaces used by outer layers
- projection-backed read services and read models

Canonical feature structure:

```text
LastMile.TMS.Application/
  Common/
    Behaviors/
    Interfaces/
  Depots/
    Commands/
    Queries/
    Reads/
  Drivers/
  Parcels/
  Routes/
  Users/
  Vehicles/
  Zones/
```

Rules:
- `Commands/` is for state-changing use cases
- `Queries/` is for complex, aggregate, or workflow-specific reads
- `Reads/` is for projection-backed GraphQL read services returning `IQueryable<FeatureReadModel>`
- handlers coordinate through `IAppDbContext` and application interfaces
- read models are query-facing models, not domain entities

### `LastMile.TMS.Domain`
This is the inner model of the system.

Responsibilities:
- entities
- enums
- base abstractions
- domain-level concepts that must remain framework-independent

What does not belong here:
- MediatR requests
- EF Core configurations
- GraphQL types
- REST contracts

### `LastMile.TMS.Infrastructure`
This is the implementation layer for non-database external concerns.

Responsibilities:
- current user resolution
- geocoding
- zone support
- email sending
- background job scheduling
- OpenIddict server and validation wiring
- options binding for external concerns

Rules:
- implement interfaces declared in `Application`
- keep transport concerns out
- do not depend on `Api` or `Persistence`

### `LastMile.TMS.Persistence`
This is the database layer.

Responsibilities:
- `AppDbContext`
- EF Core configuration classes
- migrations
- identity persistence wiring
- OpenIddict EF Core storage wiring
- database seeding

Rules:
- own schema and EF mappings
- expose persistence to `Application` through `IAppDbContext`
- do not contain transport code

### `tests/*`
Tests are split by layer so that failure ownership stays obvious.

Responsibilities:
- `LastMile.TMS.Api.Tests`: GraphQL and REST contract coverage
- `LastMile.TMS.Application.Tests`: handler, validator, and read-service coverage
- `LastMile.TMS.Architecture.Tests`: dependency and convention enforcement
- `LastMile.TMS.Domain.Tests`: pure domain behavior
- `LastMile.TMS.Infrastructure.Tests`: adapter behavior

## Dependency Rules
Dependency direction is enforced by architecture tests.

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
- `Application` uses `IAppDbContext` and application interfaces, not concrete persistence or transport details
- `Api` depends on `Application` contracts, not on feature internals that should stay behind handlers or read services
- `Persistence` and `Infrastructure` provide implementations for abstractions declared inward

## Request Flow

### REST Flow
REST is reserved for auth and system endpoints.

```text
HTTP request
-> Controller
-> transport validation and auth boundary
-> application service or auth component
-> HTTP response
```

REST rules:
- keep controllers thin
- do not add domain CRUD endpoints in REST by default
- auth endpoints such as `/connect/token` remain REST

### GraphQL Mutation And Complex Query Flow

```text
GraphQL request
-> Query or mutation resolver
-> ISender
-> FluentValidation pipeline
-> Application handler
-> IAppDbContext and/or application service interfaces
-> Persistence / Infrastructure implementation
-> DTO
-> GraphQL response
```

Use this flow for:
- all mutations
- auth-adjacent domain queries such as `viewer` / `currentUser`
- aggregate views
- workflow-specific queries
- reads with non-trivial authorization or orchestration

### GraphQL Projection-Backed Read Flow

```text
GraphQL request
-> Query resolver
-> IFeatureReadService
-> IQueryable<FeatureReadModel>
-> UseOffsetPaging / UseProjection / UseFiltering / UseSorting
-> GraphQL response
```

Use this flow for:
- flat list views
- simple detail views
- reference data and lookup-style reads
- cases where GraphQL selection, filtering, sorting, and paging should shape the query

## GraphQL Conventions
- Keep root `Query` and `Mutation` types empty and extend them by feature.
- Keep GraphQL inputs and schema types in `Api/GraphQL/<Domain>`.
- Keep business logic out of resolvers.
- For projection-backed collection fields, prefer:
  - `[UseOffsetPaging(IncludeTotalCount = true)]`
  - `[UseProjection]`
  - `[UseFiltering]`
  - `[UseSorting]`
- Return plain lists only for bounded lookups and genuinely small collections.
- Use DataLoader only when a field cannot be satisfied efficiently by projection and would otherwise cause N+1 behavior.

## Default Read Strategy By Feature
The default long-term read pattern is:

- Projection-backed by default:
  - `depots`
  - `zones`
  - `drivers`
  - small reference lookups
- MediatR-backed by default:
  - `users`
  - `routes`
  - `vehicles`
  - workflow-specific parcel queries
  - dashboards, aggregates, history views, and bundled lookups

This is a default, not a hard domain rule. The real choice is driven by read complexity.

## Validation And Error Handling

### Validation
Validation is centralized in the application pipeline.

Rules:
- use FluentValidation for commands and MediatR-backed queries
- keep business validation out of transport layers
- keep projection-backed read filtering and paging validation at the schema boundary or read service boundary as needed

### REST Error Handling
REST uses RFC 7807 Problem Details for auth and system endpoints.

### GraphQL Error Handling
GraphQL uses dedicated GraphQL error filters and should not rely on REST exception formatting.

Rules:
- GraphQL error filters live in `Api/GraphQL/Common`
- application exceptions should still originate from the use-case layer when business rules fail

## Authentication And Authorization
Authentication and authorization are runtime concerns enforced at transport boundaries.

Current model:
- OpenIddict issues and validates tokens
- `/connect/token` stays REST
- GraphQL is protected through JWT auth and resolver-level role checks

Rules:
- enforce auth at controller or resolver boundaries
- use `ICurrentUserService` inside use cases when identity is needed
- move user-self reads such as `me` toward GraphQL `viewer` / `currentUser` instead of adding more REST business endpoints

## Persistence And Data Model
`Persistence` owns the EF Core model and schema configuration.

Rules:
- schema changes require migrations
- entity configuration belongs in `Persistence/Configurations`
- `Application` must not depend on EF configuration details
- queryable read services may compose EF-backed expressions through `IAppDbContext`, but that logic still belongs in `Application`

## What Belongs Where

### Put code in `Api` when
- it is about GraphQL schema, transport auth, middleware, or REST system endpoints
- it defines resolvers, GraphQL types, controllers, or error filters

### Put code in `Application` when
- it defines a command, query, handler, validator, DTO, read service, or read model
- it coordinates business workflows
- it shapes data for domain-facing use cases

### Put code in `Domain` when
- it is a pure entity, enum, or domain abstraction

### Put code in `Infrastructure` when
- it talks to external services or the runtime environment

### Put code in `Persistence` when
- it configures EF Core, owns migrations, or defines database storage behavior

## Disallowed Patterns
- business logic inside controllers
- business logic inside GraphQL resolvers
- direct `AppDbContext` access from `Api`
- exposing domain entities directly through GraphQL
- mixing `ISender` and direct query composition inside the same resolver field
- keeping parallel REST and GraphQL domain endpoints without an active consumer
- EF Core configuration in `Api` or `Application`

## Testing Strategy
Every layer should be covered at the level where it makes decisions.

Recommended coverage:
- architecture tests for dependency and resolver rules
- application tests for handlers, validators, and read services
- API tests for GraphQL and REST contracts
- infrastructure tests for adapters
- domain tests for pure model behavior
- schema snapshot checks for GraphQL contract changes

When adding a new feature:
1. decide whether the read side is projection-backed or MediatR-backed
2. add or update application tests first
3. add API tests for the exposed GraphQL or REST contract
4. update architecture tests if a new convention is introduced intentionally

## Refactor Guidance
If you are unsure where new code belongs, follow this order:
1. model the use case in `Application`
2. decide whether reads should be handler-backed or projection-backed
3. add or reuse domain entities and enums in `Domain`
4. add persistence wiring in `Persistence` if storage changes are required
5. add infrastructure adapters in `Infrastructure` if an external capability is needed
6. expose the capability through GraphQL in `Api`

This keeps the backend centered on use cases and read strategy instead of transport convenience.
