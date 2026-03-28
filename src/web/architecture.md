# Web Architecture Guide

This document defines the target architecture for `src/web/src`.
It describes the structure we actively maintain, even if some current files still reflect an older layout.

## Core Principles

1. Keep GraphQL schema-driven. Frontend GraphQL types should come from generated operation artifacts, not handwritten transport types.
2. Keep data flow consistent:
   page or component -> query hook -> service -> typed GraphQL document -> transport client.
3. Keep `app/` thin. Routes compose pages; they do not own feature logic.
4. Keep `components/ui` primitive-only. Business behavior belongs in domain components.
5. Keep `queries/` as the home of TanStack Query hooks, keys, and cache behavior.
6. Keep `services/` thin. Services orchestrate variables, auth tokens, and minor view-model transforms; they do not define the GraphQL contract.
7. Keep `types/` for UI-local types, form models, and non-GraphQL contracts. Do not duplicate GraphQL response types there.
8. Use backend domain names as the canonical vocabulary:
   `depots`, `drivers`, `parcels`, `routes`, `users`, `vehicles`, `zones`.

## Stack

- Framework: Next.js 16 App Router
- UI: React 19, Tailwind CSS 4, shadcn primitives
- Server state: TanStack Query
- Forms: react-hook-form + Zod
- Auth: NextAuth credentials flow against OpenIddict
- API transport: GraphQL for domain operations, REST for auth/system endpoints
- Tests: Vitest + Playwright

## Canonical Folder Tree

```text
src/
  app/
    (auth)/
    (dashboard)/
    api/
    layout.tsx
    page.tsx
    providers.tsx

  components/
    auth/
    dashboard/
    depots/
    feedback/
    form/
    layout/
    list/
    routes/
    users/
    vehicles/
    zones/
    ui/

  graphql/
    generated/
    operations/

  hooks/
    use-debounce.ts
    use-floating-dropdown-position.ts

  lib/
    auth.ts
    utils.ts
    navigation/
    network/
    query/
    toast/
    validation/

  mocks/
  queries/
  services/
  types/
  proxy.ts
```

## Layer Contracts

### `app/`

Purpose:
- route files
- layouts
- top-level providers
- redirects and route entry points

Rules:
- keep route files thin
- move tables, dialogs, forms, and page clients into `components/`
- keep auth and dashboard route groups focused on composition

### `components/`

Purpose:
- reusable product UI
- domain presentation
- page-level client components extracted from `app/`

Rules:
- `components/ui` stays primitive-only
- domain-aware components live in the matching domain folder
- shared product UI should move to the narrowest folder that matches its responsibility
- do not reintroduce `components/common`

### `queries/`

Purpose:
- TanStack Query hooks
- query keys
- mutations and cache invalidation

Rules:
- components use query hooks instead of calling services directly
- query modules are domain-based: `queries/users.ts`, `queries/zones.ts`, and so on
- query hooks own cache policy and mutation side effects

### `services/`

Purpose:
- domain API orchestration
- variable preparation
- token-aware request execution
- light normalization when the UI deliberately diverges from GraphQL shapes

Rules:
- services do not import React
- services do not define raw GraphQL strings
- services consume generated operation types and typed documents
- services do not duplicate response types that already exist in generated GraphQL artifacts
- enum mapping belongs in services only when the UI intentionally uses a different local representation

### `graphql/`

Purpose:
- GraphQL operation documents
- generated GraphQL artifacts

Structure:
- `graphql/operations/`: operation documents grouped by domain
- `graphql/generated/`: codegen output, typed documents, generated types

Rules:
- operation documents are the source of truth for frontend GraphQL contracts
- generated artifacts must not be edited by hand
- do not keep manual field-string fragments or handwritten document constants once codegen is in place

### `hooks/`

Purpose:
- generic hooks that are not tied to one backend domain

Rules:
- keep only reusable React or browser hooks here
- entity data hooks belong in `queries/`, not `hooks/`

### `lib/`

Purpose:
- cross-cutting helpers that are not components and not query hooks

Allowed subfolders:
- `lib/network/`: low-level network and GraphQL transport helpers
- `lib/query/`: query client helpers
- `lib/navigation/`: app shell and nav helpers
- `lib/validation/`: Zod schemas and validators
- `lib/toast/`: application toast wrapper

Rules:
- no React hooks in `lib/`
- no entity-specific data fetching in `lib/`
- do not recreate catch-all folders like `helpers`, `common`, or `api`

### `types/`

Purpose:
- UI-local types
- form models
- route params
- non-GraphQL shared types

Rules:
- do not store GraphQL response contracts here
- generated GraphQL enums and payload types are the source of truth for transport types
- if a UI model intentionally differs from the GraphQL shape, define that transformed model here

## Allowed Import Direction

Preferred direction:

```text
app -> components -> queries -> services -> graphql/generated
                         |          |
                         v          v
                        types      lib
```

Allowed:
- `app` may import `components`, `lib`, `types`, and auth helpers
- `components` may import `queries`, `hooks`, `lib`, `types`, and UI primitives
- `queries` may import `services`, `types`, and `lib/query`
- `services` may import generated GraphQL artifacts, `lib/network`, `lib/*` pure helpers, and UI-local types

Do not do this:
- `services` importing `queries`
- `graphql` importing `services`
- `components/ui` importing domain services
- `types/` becoming a second GraphQL schema mirror

## Data Flow

Standard request flow:

1. A page or client component reads route params and UI state.
2. The component calls a domain hook from `queries/`.
3. The query hook calls a function from `services/`.
4. The service calls the shared GraphQL transport with a typed document and typed variables.
5. Generated GraphQL result types flow back into the service.
6. The service optionally derives a small UI-specific shape if the UI truly needs one.
7. The query hook manages caching and invalidation.
8. The component renders UI and mutation states.

Auth is the main exception:
- NextAuth and token refresh still use REST auth endpoints
- domain reads and writes should not go through REST by default

## GraphQL Codegen And Type Contracts

The backend exposes domain entities directly through GraphQL using `EntityObjectType<T>` with `BindFieldsExplicitly()`. This means:

- The GraphQL schema shape is controlled on the backend by explicit field declarations.
- Frontend generated types from codegen accurately reflect what the backend exposes.
- No manual type mirroring is needed on the frontend — generated artifacts are the source of truth.

When the backend adds or removes a field from an `EntityObjectType<T>`, run codegen to regenerate frontend types:

```bash
cd src/web && npm run codegen
```

Never handwrite types that duplicate GraphQL response shapes. If a UI component needs a differently shaped object, derive it in `services/` and declare the local type in `types/`.

## Adding a New Domain Feature — Checklist

When adding frontend support for a new domain entity (e.g. `Vehicle`):

1. Add GraphQL operation documents under `graphql/operations/<domain>/`.
2. Run `npm run codegen` to generate typed documents and result types.
3. Add a service in `services/<domain>.service.ts` consuming generated documents.
4. Add TanStack Query hooks in `queries/<domain>.ts`.
5. Add domain components in `components/<domain>/`.
6. Add route pages in `app/(dashboard)/<domain>/` — keep them thin.
7. Add Zod validation schemas in `lib/validation/` if forms are involved.
8. Write Vitest unit tests next to service and query files.
9. Update Playwright e2e flows if the domain has CRUD that affects main navigation flows.

## Naming Rules
- Use backend domain names for module names: `users`, not `user-management`.
- Use kebab-case for file names except where framework conventions require otherwise.
- Prefer generated GraphQL enum names and field names over local mirrors.
- Keep view-model names explicit when they intentionally differ from transport names.

## What Belongs Where

### `components/ui`
Belongs here:
- buttons, inputs, cards, dialogs, dropdown primitives

Does not belong here:
- domain filters
- entity-specific rows or cells
- query-state wrappers with business meaning

### `queries`
Belongs here:
- `useUsers`
- `useZones`
- query keys
- cache invalidation rules

Does not belong here:
- raw GraphQL strings
- response type declarations that belong to generated artifacts

### `services`
Belongs here:
- `users.service.ts`
- `zones.service.ts`
- token-aware request orchestration
- small UI-facing transforms

Does not belong here:
- React hooks
- JSX
- handwritten GraphQL contract definitions

### `graphql`
Belongs here:
- operation documents
- generated typed documents
- generated result and variable types

Does not belong here:
- fetch logic
- manual enum maps
- domain normalization helpers

### `types`
Belongs here:
- form state
- filter UI state
- app-local transport wrappers that are not GraphQL schema types

Does not belong here:
- copies of GraphQL response objects
- copies of GraphQL enums unless the UI intentionally uses a different model

## Deprecated Structure
These patterns are deprecated and should be removed during migration:

- handwritten `graphql/*.ts` document strings
- duplicated GraphQL payload types in `types/*`
- global enum conversion maps that only exist because frontend types drifted from the GraphQL schema
- permissive response mappers that accept both camelCase and PascalCase for the same GraphQL field
- `components/common`
- `lib/api`
- `lib/hooks`

## Testing Rules
- Unit and component tests live next to the code they cover or in the owning layer test folder.
- Structural refactors must keep tests aligned with the new canonical import paths.
- Minimum validation for frontend architecture work:
  - `npm run build`
  - `npm run test:run`
- Main e2e flows should continue covering:
  - login
  - navigation
  - users
  - vehicles
  - depots and zones when their CRUD flow changes

## Refactor Checklist
Use this checklist when adding or moving code:

1. Pick the backend-aligned domain name.
2. Keep route composition in `app/`.
3. Put React Query logic in `queries/`.
4. Put request orchestration in `services/`.
5. Put operation documents under `graphql/operations/`.
6. Use generated GraphQL artifacts as the transport source of truth.
7. Define local UI types only when the UI genuinely needs a shape different from GraphQL.
