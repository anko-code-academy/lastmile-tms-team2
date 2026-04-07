# Web Architecture Guide

## Purpose
This document describes the accepted architecture for `src/web/src`.
It reflects the structure we maintain today. We are not forcing a large feature-folder rewrite right now.

## Current Position
The frontend is acceptable as a layered Next.js app with domain-based modules spread across shared folders:
- `app/` for routing and composition
- `components/` for page and UI building blocks
- `queries/` for TanStack Query hooks
- `services/` for request orchestration
- `graphql/` for documents, generated artifacts, and domain re-export modules
- `types/` for UI-local contracts

The goal is consistency inside this layout, not a broad move to `features/*` at this time.

## Core Principles
- Keep routes thin. `app/` should compose pages, not hold feature behavior.
- Keep GraphQL schema-driven. Generated GraphQL artifacts are the transport source of truth.
- Keep components talking to query hooks, not directly to services.
- Keep services thin. They prepare variables, auth tokens, mock branching, and small UI-facing normalization.
- Keep domain vocabulary aligned with the backend:
  - `bin-locations`
  - `depots`
  - `drivers`
  - `parcels`
  - `routes`
  - `users`
  - `vehicles`
  - `zones`
- Keep shared UI truly shared. Domain-specific UI belongs in the owning domain folder.
- Prefer local UI models only when they add value beyond raw GraphQL shapes.

## Stack
- Next.js 16 App Router
- React 19
- Tailwind CSS 4
- TanStack Query
- NextAuth
- GraphQL codegen
- Vitest for unit/component testing
- Playwright for end-to-end flows

## Canonical Folder Tree

```text
src/web/src/
  app/
    (auth)/
    (dashboard)/
    admin/
    api/
    layout.tsx
    page.tsx
    providers.tsx

  components/
    auth/
    bin-locations/
    dashboard/
    depots/
    drivers/
    detail/
    feedback/
    form/
    layout/
    list/
    routes/
    ui/
    users/
    vehicles/
    zones/

  graphql/
    documents/
    generated/
    bin-locations.ts
    depots.ts
    drivers.ts
    parcels.ts
    routes.ts
    users.ts
    vehicles.ts
    zones.ts

  hooks/
  lib/
  mocks/
  queries/
  services/
  types/
```

## Layer Responsibilities

### `app/`
Purpose:
- route files
- route-group layouts
- top-level providers
- redirects and auth gates

Rules:
- route files stay thin
- page files usually just render a page component from `components/`
- server-side auth checks are allowed here when the page needs a token or role gate

Examples:
- thin composition page: [page.tsx](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/app/(dashboard)/vehicles/page.tsx)
- server-auth page passing token downward: [page.tsx](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/app/(dashboard)/users/page.tsx)

### `components/`
Purpose:
- page-level UI
- domain presentation
- reusable product components
- shared primitives in `components/ui`

Rules:
- `components/ui` is primitive-only
- domain-specific pages and dialogs live under the matching domain folder
- shared table/detail/form shells live in the matching shared folder, not in `ui`

Examples:
- domain page: [vehicles-page.tsx](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/components/vehicles/vehicles-page.tsx)
- domain page: [drivers-page.tsx](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/components/drivers/drivers-page.tsx)
- domain-heavy admin page: [user-management-page.tsx](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/components/users/user-management-page.tsx)

### `queries/`
Purpose:
- TanStack Query hooks
- cache keys
- invalidation policy
- mutation success behavior

Rules:
- components call hooks from `queries/`
- hooks call services from `services/`
- query modules are domain-based

Accepted auth patterns:
- hook reads auth state itself for normal dashboard flows
- hook receives `accessToken` when a server page already resolved auth

Both patterns are acceptable today. Do not mix them inside the same domain module without a reason.

Examples:
- session-aware hooks: [vehicles.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/queries/vehicles.ts)
- session-aware hooks: [drivers.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/queries/drivers.ts)
- token-parameter hooks: [users.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/queries/users.ts)

### `services/`
Purpose:
- domain request orchestration
- token-aware GraphQL calls
- mock-vs-real branching
- light normalization into stable UI models

Rules:
- no React in services
- no JSX in services
- services do not own cache behavior
- services should consume typed GraphQL documents or domain GraphQL barrel exports
- small UI-facing normalization is allowed

Examples:
- normalized service object style: [vehicles.service.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/services/vehicles.service.ts)
- normalized service object style: [drivers.service.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/services/drivers.service.ts)
- function-based service style: [users.service.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/services/users.service.ts)

Both export styles are acceptable. Prefer consistency within a given domain over repo-wide mechanical rewrites.

### `graphql/`
Purpose:
- `.graphql` operation documents
- generated codegen output
- stable domain re-export modules

Structure:
- `graphql/documents/`: operation documents grouped by domain file
- `graphql/generated/`: codegen output
- `graphql/<domain>.ts`: stable import surface re-exporting generated documents and types

Rules:
- `.graphql` files are the source of truth for frontend GraphQL operations
- generated artifacts are never hand-edited
- prefer importing domain-level GraphQL barrels from services instead of spreading `generated` imports everywhere

Examples:
- document file: [vehicles.graphql](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/graphql/documents/vehicles.graphql)
- document file: [drivers.graphql](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/graphql/documents/drivers.graphql)
- domain re-export: [vehicles.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/graphql/vehicles.ts)
- domain re-export: [drivers.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/graphql/drivers.ts)

### `types/`
Purpose:
- UI-local models
- form payloads
- request types passed between component, hook, and service layers
- local shapes that intentionally differ from raw GraphQL transport shapes

Rules:
- generated GraphQL types remain the source of truth for transport contracts
- local `types/*` are allowed when the service normalizes GraphQL data into a UI model
- avoid creating a second schema mirror without value

Examples:
- local UI model around generated enums: [vehicles.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/types/vehicles.ts)
- local UI model for forms and commands: [drivers.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/types/drivers.ts)
- user management local request and view models: [users.ts](/C:/Users/mesut/source/repos/lastmile-tms-team2/src/web/src/types/users.ts)

### `hooks/`
Purpose:
- generic reusable hooks not tied to one backend domain

Rules:
- domain data hooks belong in `queries/`
- keep `hooks/` for generic React/browser behavior only

### `lib/`
Purpose:
- cross-cutting helpers that are not components and not query hooks

Current accepted subfolders include:
- `lib/network/`
- `lib/query/`
- `lib/navigation/`
- `lib/validation/`
- `lib/toast/`
- small domain helper folders such as `lib/depots/`, `lib/labels/`, `lib/parcels/`, or `lib/time/` when the helper is not a service and not UI

Rules:
- keep helpers small and focused
- do not move domain API orchestration into `lib`
- no React hooks in `lib`

## Data Flow

### Standard dashboard flow

```text
app page
-> domain component
-> query hook
-> service
-> graphql/<domain>.ts re-export
-> graphql/generated typed document
-> shared GraphQL client
-> response
-> optional service normalization
-> query cache
-> component render
```

### Server-auth page flow

```text
server page in app/
-> auth / role gate
-> pass accessToken to client component
-> query hook(accessToken)
-> service(accessToken)
-> GraphQL client
```

This pattern is valid for admin-style pages such as users.

## Naming and Structure Rules
- Use backend feature names for domains.
- Keep file names kebab-case in the web app unless framework conventions require otherwise.
- Keep route pages thin and domain pages in `components/<domain>/`.
- Keep one domain module per file in `queries/`, `services/`, and `graphql/documents/`.
- Use domain GraphQL barrel files as stable imports from services.

## What We Intentionally Keep As-Is
- We are not forcing a move to `features/*` right now.
- We are not rewriting every service to the same export style unless there is a concrete need.
- We accept both:
  - session-aware query hooks
  - token-parameter query hooks
- We accept local UI/request types in `types/` when they represent a deliberate UI model instead of raw transport.

The standard here is operational consistency, not maximal structural purity.

## Preferred Direction for New Code
- add new page entrypoints in `app/`
- add domain UI in `components/<domain>/`
- add cache logic in `queries/<domain>.ts`
- add request orchestration in `services/<domain>.service.ts`
- add `.graphql` operations in `graphql/documents/<domain>.graphql`
- regenerate `graphql/generated/`
- add local UI types only when needed

## Avoid
- direct service calls from components
- raw GraphQL strings inside services
- editing generated GraphQL files by hand
- putting business page logic into `app/` route files
- moving shared primitives into domain folders
- creating broad catch-all folders such as `common`, `helpers`, or `api` when a narrower existing folder fits

## Verification

Recommended frontend verification after structural or architectural changes:

```bash
cd src/web
npm run codegen
npm run test:run
npm run build
```

Add Playwright coverage when navigation or core CRUD flows change.
