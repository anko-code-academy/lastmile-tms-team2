# Architecture Migration Roadmap

This document captures the planned migration path toward the target backend and web architecture.

## Locked Decisions
- Scope: `backend + web`
- Primary API style: GraphQL for domain operations
- REST scope: auth and system endpoints only
- Migration style: phased refactor
- Read strategy: hybrid
- Frontend transport strategy: generated GraphQL types and typed documents

## Target End State

### Backend
- GraphQL is the main domain contract.
- Commands and complex reads use `CQRS + MediatR`.
- Simple read-only queries use projection-backed read services and HotChocolate middleware.
- Domain entities are not exposed directly through GraphQL.
- REST remains only for auth and technical endpoints.

### Web
- GraphQL operations are schema-driven.
- Generated GraphQL artifacts are the source of truth for transport types.
- Components talk only to query hooks.
- Services stay thin and stop duplicating schema types.

## Migration Phases

### Phase 0: Stabilize Baseline
- fix backend API test-host instability
- keep existing behavior stable while introducing the migration scaffolding
- capture or document the current GraphQL contract before broad refactors

### Phase 1: Backend Conventions
- add and standardize projection support in GraphQL
- formalize the two read paths:
  - MediatR-backed complex reads
  - projection-backed simple reads
- add architecture tests for resolver conventions
- keep REST limited to auth and system behavior

### Phase 2: Web GraphQL Foundation
- introduce generated GraphQL artifacts
- move away from handwritten GraphQL document constants
- stop duplicating GraphQL payload types in `src/web/src/types`
- keep the shared GraphQL transport client as the single fetch boundary

### Phase 3: Simple Feature Migration
- migrate flat lookup and CRUD-heavy read domains first
- preferred early candidates:
  - depots
  - zones
  - drivers
  - simple reference lookups

### Phase 4: Complex Feature Migration
- keep complex, aggregate-heavy, or workflow-heavy reads on MediatR
- expected long-term MediatR-backed domains:
  - users
  - routes
  - vehicles
  - workflow-specific parcel reads

### Phase 5: Contract Cleanup
- remove legacy handwritten GraphQL transport patterns in web
- remove legacy REST domain endpoints that no longer have active clients
- keep only the transport surfaces that match the target architecture

## Working Rules During Migration
- do not mix direct query composition and `ISender` inside the same resolver field
- do not expose domain entities through GraphQL
- do not add new handwritten frontend transport types for GraphQL payloads
- prefer one canonical transport per feature
- keep backend and web changes paired for any intentional GraphQL contract change

## Done Criteria
- GraphQL is the default domain API
- REST is limited to auth and technical endpoints
- frontend transport types come from generated GraphQL artifacts
- simple reads use HotChocolate data middleware
- complex reads use MediatR handlers
- architecture tests enforce the agreed boundaries
