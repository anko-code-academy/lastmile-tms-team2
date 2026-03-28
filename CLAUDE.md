# CLAUDE.md

## Core Rules

### 1. TDD First
- For every feature, bug fix, or behavior change, follow `Red -> Green -> Refactor`.
- Start by writing a failing test that proves the expected behavior.
- Implement the smallest change that makes the test pass.
- Refactor only after the tests are green again.
- If the task is documentation, config-only, or another change that cannot be meaningfully driven by a test, say that explicitly and run the closest relevant verification instead.

### 2. Read Architecture Before Coding
- For backend tasks, read [src/backend/architecture.md](/C:/Users/User/source/repos/lastmile-tms-team2/src/backend/architecture.md).
- For web/frontend tasks, read [src/web/architecture.md](/C:/Users/User/source/repos/lastmile-tms-team2/src/web/architecture.md).
- Do not introduce structure or dependencies that conflict with those documents.
- If code and architecture docs diverge, move the code toward the documented target state and call out the mismatch.

### 3. Conventions
- Preserve the project vocabulary: `depots`, `drivers`, `parcels`, `routes`, `users`, `vehicles`, `zones`.
- Keep transport and composition layers thin. Business logic belongs in the owning application/domain layer.
- Prefer small, focused files and clear responsibilities over large mixed modules.
- Update tests together with code changes.
- Update architecture docs when architectural rules or structure change.
- Follow repository formatting conventions: C# uses 4 spaces, TypeScript uses 2 spaces, line endings are LF.

## Verification
- Backend: `cd src/backend && dotnet test`
- Web: `cd src/web && npm run test:run && npm run build`
- Mobile: `cd src/mobile && npx tsc --noEmit`
- Run the smallest relevant test scope first, then broader verification before finishing.

## Repo Map
- `src/backend` - .NET backend
- `src/web` - Next.js web app
- `src/mobile` - Expo mobile app

### Adding a New Backend Domain Entity — Full Checklist

1. **Domain**: add entity to `Domain/Entities/`.
2. **Persistence**: add `DbSet<T>` to `IAppDbContext` and `AppDbContext`, add EF configuration in `Persistence/Configurations/`, create migration.
3. **Application/DTOs**: create `<Entity>Dto.cs` and related DTOs.
4. **Application/Mapper**: create `<Domain>Mapper.cs` with Mapperly partial methods.
5. **Application/Reads**: create `I<Domain>ReadService` + `<Domain>ReadService` returning `IQueryable<Entity>` — `AsNoTracking()` only.
6. **Application/Commands**: create commands + handlers using `dto.ToEntity()` and `entity.ToDtoMapped()`.
7. **Application/Queries**: create MediatR queries + handlers if complex reads are needed.
8. **Application/Validators**: create FluentValidation validators for commands.
9. **Application Tests**: write handler, validator, read service tests.
10. **Api/GraphQL Types**: create `<Entity>Type : EntityObjectType<Entity>` with explicit fields.
11. **Api/GraphQL InputMapper**: create `<Domain>InputMapper.cs` with Mapperly for `Input → Dto`.
12. **Api/GraphQL Query**: create `<Domain>Query` with correct attribute order.
13. **Api/GraphQL Mutation**: create `<Domain>Mutation` returning `Entity`.
14. **Api Tests**: write GraphQL contract tests.
15. **Architecture Tests**: update if a new convention is introduced.

### Adding a New Frontend Domain Feature — Full Checklist

1. **GraphQL operations**: add `.graphql` operation files under `graphql/operations/<domain>/`.
2. **Codegen**: run `npm run codegen` to generate typed documents and result types in `graphql/generated/`.
3. **Service**: add `services/<domain>.service.ts` consuming generated typed documents.
4. **Query hooks**: add `queries/<domain>.ts` with TanStack Query hooks.
5. **Components**: add domain components under `components/<domain>/`.
6. **Routes**: add pages under `app/(dashboard)/<domain>/` — keep route files thin.
7. **Validation**: add Zod schemas in `lib/validation/` for any forms.
8. **Tests**: add Vitest unit tests next to service and query files.
9. **E2e**: update Playwright flows if CRUD affects main navigation.

### Key Rules
- Never handwrite types that duplicate GraphQL response shapes — use generated artifacts.
- Never call services directly from components — always go through query hooks.
- Never put business logic in route files — move it to components or services.
- Run `npm run codegen` after any backend GraphQL schema change before writing frontend code.
