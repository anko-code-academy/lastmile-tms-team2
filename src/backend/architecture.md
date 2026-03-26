# Backend Architecture

## Purpose
This document defines the target architecture for `src/backend`. It describes how the backend is structured, which responsibilities belong to each project, which dependency directions are allowed, and how requests move through the system.

This file documents the intended steady state of the backend. New code should follow these rules even if older code still needs cleanup.

## Scope
The backend solution is a layered .NET application that exposes both REST and GraphQL APIs over the same application core.

Primary backend domains:
- `depots`
- `drivers`
- `parcels`
- `routes`
- `users`
- `vehicles`
- `zones`

## Core Principles
- Keep transport thin. Controllers and GraphQL resolvers should delegate to application commands and queries instead of containing business logic.
- Keep business workflows in `Application`. Request orchestration, validation, DTO shaping, and use-case logic belong there.
- Keep domain types clean. `Domain` owns entities, enums, and domain-level base abstractions without depending on outer layers.
- Depend inward only. Outer layers may reference inner layers. Inner layers must not reference transport or infrastructure concerns.
- Make infrastructure replaceable. External integrations are behind interfaces declared in `Application` and implemented in `Infrastructure` or `Persistence`.
- Keep persistence explicit. Entity Framework Core mappings, migrations, and data access wiring live in `Persistence`.
- Test architecture as code. Dependency boundaries are enforced by dedicated architecture tests.

## Solution Layout
The canonical solution layout is:

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
- REST controllers
- GraphQL schema, types, queries, and mutations
- HTTP auth and authorization setup defaults
- CORS, Swagger, Problem Details, and request logging
- transport-specific error handling

What belongs here:
- `Program.cs`
- `ServiceCollectionExtensions.cs`
- `Controllers/*`
- `GraphQL/*`
- `GlobalExceptionHandler.cs`

What does not belong here:
- business rules
- Entity Framework mappings
- direct integration logic for email, geocoding, or background execution

### `LastMile.TMS.Application`
This is the use-case layer and the main home of backend behavior.

Responsibilities:
- commands, queries, and handlers
- DTOs returned to transport layers
- validators
- MediatR pipeline behaviors
- application interfaces used by outer layers
- feature-level orchestration

Canonical feature structure:

```text
LastMile.TMS.Application/
  Common/
    Behaviors/
    Constants/
    Helpers/
    Interfaces/
  Depots/
    Commands/
    DTOs/
    Queries/
    Validators/
  Drivers/
  Parcels/
    Services/
  Routes/
  Users/
    Common/
  Vehicles/
  Zones/
    Services/
```

Rules:
- feature folders are organized by business domain
- handlers coordinate use cases through `IAppDbContext` and application interfaces
- validators live next to their feature commands and queries
- external dependencies are represented as interfaces, not concrete implementations

### `LastMile.TMS.Domain`
This is the inner model of the system.

Responsibilities:
- entities
- enums
- base entity abstractions
- domain event interfaces

What belongs here:
- `Entities/*`
- `Enums/*`
- `Common/BaseEntity.cs`
- `Common/BaseAuditableEntity.cs`

What does not belong here:
- MediatR requests
- EF Core configurations
- API contracts
- infrastructure service implementations

### `LastMile.TMS.Infrastructure`
This is the implementation layer for non-database external concerns.

Responsibilities:
- current user resolution
- geocoding
- zone boundary parsing
- zone matching support
- email sending
- background job scheduling
- options binding for infrastructure concerns
- OpenIddict server and validation wiring

Canonical structure:

```text
LastMile.TMS.Infrastructure/
  Configuration/
  Options/
  Services/
  DependencyInjection.cs
```

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

Canonical structure:

```text
LastMile.TMS.Persistence/
  Configurations/
  Migrations/
  AppDbContext.cs
  DbSeeder.cs
  DependencyInjection.cs
```

Rules:
- own database configuration and schema evolution
- expose persistence to the application layer through `IAppDbContext`
- do not contain transport code or infrastructure service implementations

### `tests/*`
Tests are split by layer so that failure ownership stays obvious.

Responsibilities:
- `LastMile.TMS.Api.Tests`: transport and integration coverage for REST and GraphQL
- `LastMile.TMS.Application.Tests`: handler, validator, and application-rule coverage
- `LastMile.TMS.Architecture.Tests`: dependency-boundary enforcement
- `LastMile.TMS.Domain.Tests`: pure domain behavior coverage
- `LastMile.TMS.Infrastructure.Tests`: infrastructure service behavior coverage

## Dependency Rules
Dependency direction is not just a convention. It is enforced by `LastMile.TMS.Architecture.Tests`.

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

Disallowed examples:
- `Domain -> Application`
- `Application -> Infrastructure`
- `Application -> Persistence`
- `Infrastructure -> Persistence`
- `Persistence -> Api`

Practical rule:
- if a feature needs data access, `Application` depends on an abstraction such as `IAppDbContext`
- if a feature needs an external capability, `Application` defines the interface and `Infrastructure` implements it
- if a feature needs transport-specific serialization, that belongs in `Api`

## Request Flow

### REST Flow
The REST path should follow this sequence:

```text
HTTP request
-> Controller
-> MediatR command/query
-> FluentValidation pipeline
-> Application handler
-> IAppDbContext and/or application service interfaces
-> Persistence / Infrastructure implementation
-> DTO or status result
-> HTTP response
```

REST controller rules:
- keep controllers thin
- authorize at the endpoint boundary
- translate transport inputs into commands and queries
- return transport-friendly results
- use Problem Details for validation and unhandled server errors

### GraphQL Flow
The GraphQL path should follow the same core use-case pipeline:

```text
GraphQL request
-> Query or mutation resolver
-> MediatR command/query
-> FluentValidation pipeline
-> Application handler
-> IAppDbContext and/or application service interfaces
-> Persistence / Infrastructure implementation
-> DTO
-> GraphQL response
```

GraphQL rules:
- resolvers are thin and feature-scoped by domain
- schema-specific input and object types stay in `Api/GraphQL/*`
- GraphQL-specific error filters stay in `Api/GraphQL/Common`
- business logic still belongs in `Application`

## Feature Organization
The default organization pattern is domain first, then use-case type.

Example:

```text
Application/Vehicles/
  Commands/
  DTOs/
  Queries/
  Validators/
```

Use the same naming vocabulary across layers:
- `Depots`
- `Drivers`
- `Parcels`
- `Routes`
- `Users`
- `Vehicles`
- `Zones`

Do not invent parallel naming systems for the same concept in different projects.

## Validation and Error Handling

### Validation
Validation is centralized in the application pipeline.

Rules:
- use FluentValidation for request validation
- register validators from the application assembly
- run validation before handlers execute
- keep transport layers free from duplicated business validation

The current MediatR pipeline behavior is `ValidationBehavior<TRequest, TResponse>`.

### REST Error Handling
REST uses RFC 7807 Problem Details.

Current behavior:
- `FluentValidation.ValidationException` becomes `400` with `ValidationProblemDetails`
- unhandled exceptions become `500` with `ProblemDetails`
- detailed exception messages are shown only in development environments

### GraphQL Error Handling
GraphQL has a dedicated error pipeline and should not rely on the REST exception handler.

Rules:
- GraphQL error filters stay in `Api/GraphQL/Common`
- resolver code should still delegate business decisions to the application layer

## Authentication and Authorization
Authentication and authorization are part of the runtime contract, not scattered feature code.

Current model:
- OpenIddict is used for token issuing and validation
- password and refresh-token flows are enabled
- the token endpoint is `/connect/token`
- JWT validation is configured as the default authenticate and challenge scheme
- access and refresh token lifetimes are read from configuration

Authorization rules:
- enforce role checks at REST controller and GraphQL resolver boundaries
- application handlers may assume authenticated context is available through `ICurrentUserService` when needed
- transport layers should not embed business authorization logic that belongs in a use case

## Persistence and Data Model
`Persistence` owns the EF Core model and schema configuration.

Current persistence model:
- `AppDbContext` derives from `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`
- Postgres is the primary runtime database
- NetTopologySuite and PostGIS are enabled for spatial data
- test scenarios may swap the main database for `UseInMemoryDatabase`
- EF Core configurations are discovered from the persistence assembly
- migrations live in `Persistence/Migrations`
- `DbSeeder` runs as a hosted service

`AppDbContext` currently exposes sets for:
- `Addresses`
- `Depots`
- `DepotOperatingHours`
- `Zones`
- `Permissions`
- `Vehicles`
- `Drivers`
- `DriverAvailabilities`
- `Routes`
- `Parcels`

Persistence rules:
- entity configuration classes belong in `Persistence/Configurations`
- schema changes require a migration
- `Application` must not reach directly into EF configuration details

## External Services and Background Work
External integrations belong behind application-facing interfaces.

Current responsibilities implemented in `Infrastructure` include:
- current user resolution from the HTTP context
- geocoding for parcel registration
- zone boundary parsing
- zone matching
- user account email sending
- background scheduling for account email jobs

Background processing rules:
- Hangfire is the production job runner
- when test settings disable external infrastructure, in-memory substitutes are used where appropriate
- job orchestration belongs in infrastructure, not in controllers or GraphQL resolvers

## Configuration and Runtime
The backend is configuration-driven.

Important configuration areas:
- `ConnectionStrings`
- `Authentication`
- `Frontend`
- `Email`
- `Testing`
- `Serilog`

Runtime notes:
- `Program.cs` is the only application entry point
- Serilog is the logging pipeline
- CORS uses the `AllowFrontend` policy
- Swagger is enabled in development
- GraphQL is mapped on both `/api/graphql` and `/graphql`
- REST controllers and GraphQL share the same application core
- the Docker image builds and runs `LastMile.TMS.Api`

## What Belongs Where

### Put code in `Api` when
- it is about HTTP, GraphQL, middleware, auth defaults, or response formatting
- it defines controllers, GraphQL types, resolvers, or transport-specific error filters

### Put code in `Application` when
- it defines a use case
- it validates an incoming command or query
- it shapes data into DTOs
- it coordinates domain entities, persistence abstractions, and external service interfaces

### Put code in `Domain` when
- it is a pure entity, enum, or domain abstraction
- it should remain independent of framework and transport concerns

### Put code in `Infrastructure` when
- it talks to external services
- it reads current request identity
- it schedules or performs side effects outside the database

### Put code in `Persistence` when
- it maps entities to tables
- it configures EF Core
- it changes the database schema
- it seeds persistent data

## Conventions
- Prefer one feature folder per business domain in `Application`.
- Prefer one command or query per file.
- Keep handlers small and explicit.
- Keep DTOs in the application layer, not the API layer.
- Keep GraphQL inputs and types in `Api/GraphQL/<Domain>`.
- Keep controllers and resolvers thin.
- Prefer constructor injection through DI registration modules.
- Prefer configuration-bound options classes for infrastructure concerns.
- Add or update architecture tests when dependency rules change intentionally.

## Disallowed Patterns
- business logic inside controllers
- business logic inside GraphQL resolvers
- direct `Infrastructure` references from `Application`
- direct `Persistence` references from `Application` other than approved abstractions
- EF Core configuration in `Api` or `Application`
- transport DTOs leaking into `Domain`
- external service SDK usage directly inside controllers or handlers when an interface should own it

## Testing Strategy
Every layer should be covered at the level where it makes decisions.

Recommended split:
- use architecture tests to protect dependency boundaries
- use application tests for validation, handlers, and use-case rules
- use API tests for REST and GraphQL contract behavior
- use infrastructure tests for integration adapters and service implementations
- use domain tests for entity and rule behavior that does not need outer layers

When adding a new feature:
1. add or update application tests for the use case
2. add API tests for the exposed contract if the feature is public
3. add infrastructure tests if a new adapter or external integration is introduced
4. verify architecture boundaries still hold

## Refactor Guidance
If you are unsure where new code belongs, follow this order:
1. start with the use case in `Application`
2. add or reuse domain entities and enums in `Domain`
3. add persistence wiring in `Persistence` if data storage is involved
4. add infrastructure implementations in `Infrastructure` if an external capability is required
5. expose the use case through REST and/or GraphQL in `Api`

This order keeps the backend centered on business workflows instead of transport or framework details.
