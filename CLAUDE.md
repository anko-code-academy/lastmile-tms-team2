# CLAUDE.md

## Core Rules

### 1. TDD First
- For every feature, bug fix, or behavior change, follow `Red -> Green -> Refactor`.
- Start by writing a failing test that proves the expected behavior.
- Implement the smallest change that makes the test pass.
- Refactor only after tests are green again.
- If the task is documentation, config-only, or another change that cannot be meaningfully driven by a test, say that explicitly and run the closest relevant verification instead.

### 2. Read Architecture Before Coding
- For backend tasks, read [src/backend/architecture.md](src/backend/architecture.md).
- For web/frontend tasks, read [src/web/architecture.md](src/web/architecture.md).
- Do not introduce structure or dependencies that conflict with those documents.
- If code and architecture docs diverge, move the docs or code intentionally and call out the mismatch.

### 3. Conventions
- Preserve the project vocabulary:
  - `bin-locations`
  - `depots`
  - `drivers`
  - `parcels`
  - `routes`
  - `users`
  - `vehicles`
  - `zones`
- Keep transport and composition layers thin.
- Prefer small, focused files and clear responsibilities over large mixed modules.
- Update tests together with code changes.
- Update architecture docs when architectural rules or maintained structure changes.
- Follow repository formatting conventions:
  - C# uses 4 spaces
  - TypeScript uses 2 spaces
  - line endings are LF

## Verification
- Backend:
  - `dotnet build src/backend/src/LastMile.TMS.Application/LastMile.TMS.Application.csproj --no-restore`
  - `dotnet build src/backend/src/LastMile.TMS.Api/LastMile.TMS.Api.csproj --no-restore`
  - `dotnet test src/backend/tests/LastMile.TMS.Application.Tests/LastMile.TMS.Application.Tests.csproj --no-build`
  - `dotnet test src/backend/tests/LastMile.TMS.Api.Tests/LastMile.TMS.Api.Tests.csproj --no-build -- RunConfiguration.MaxCpuCount=1`
- Web:
  - `cd src/web && npm run codegen`
  - `cd src/web && npm run test:run`
  - `cd src/web && npm run build`
- Run the smallest relevant scope first, then expand as needed.

## Repo Map
- `src/backend` - .NET backend
- `src/web` - Next.js web app
- `src/mobile` - Expo mobile app

## Backend Checklist

When adding or changing a backend feature:

1. Model the use case in `Application`.
2. Choose the read path:
   - projection-backed `IQueryable<TEntity>` read service
   - MediatR-backed query handler
3. Organize `Application/<Feature>/` using:
   - `Commands/<UseCase>/`
   - `Queries/<UseCase>/`
   - `DTOs/`
   - `Mappings/`
   - `Reads/`
   - optional `Services/` or `Support/`
4. Use Mapperly for:
   - GraphQL `Input -> DTO` in `Api`
   - `DTO <-> Entity` in `Application`
5. Organize `Api/GraphQL/<Feature>/` using:
   - `*Queries.cs`
   - `*Mutations.cs`
   - `*Inputs.cs`
   - `*Mappings.cs`
   - `*Types.cs`
6. Keep GraphQL resolvers thin.
7. Add or update tests in the owning backend test project.

## Frontend Checklist

When adding or changing a frontend feature:

1. Add or update `.graphql` operations in `src/web/src/graphql/documents/`.
2. Run `cd src/web && npm run codegen`.
3. Update domain GraphQL re-export modules in `src/web/src/graphql/<domain>.ts` if needed.
4. Put request orchestration in `src/web/src/services/<domain>.service.ts`.
5. Put React Query hooks in `src/web/src/queries/<domain>.ts`.
6. Keep route files thin under `src/web/src/app/`.
7. Put domain UI in `src/web/src/components/<domain>/`.
8. Use local `src/web/src/types/*` only for UI/request models that add value beyond raw GraphQL transport types.
9. Add or update tests close to the owning layer.

## Key Rules
- Do not call backend services directly from components; go through query hooks.
- Do not put business logic in route files.
- Do not edit generated GraphQL artifacts by hand.
- Do not introduce a second hand-maintained GraphQL schema mirror in frontend types without a reason.
- Run codegen after backend GraphQL contract changes before touching frontend transport code.
