# LastMile TMS Web

Frontend for the LastMile TMS dashboard and admin flows.

## Architecture
- Next.js App Router frontend with React 19 and TanStack Query
- GraphQL is the primary transport for domain data
- OpenIddict auth endpoints remain REST and are used through NextAuth
- Target architecture and folder conventions are documented in `architecture.md`
- Migration phases for the broader repo are documented in `../../docs/architecture-roadmap.md`

## Local Development

Install dependencies and start the dev server:

```bash
npm install
npm run dev
```

Open `http://localhost:3000`.

## Quality Checks

Run the main frontend checks with:

```bash
npm run build
npm run test:run
npm run test:e2e
```

## Current Direction

The frontend is moving toward:
- schema-driven GraphQL operations
- generated GraphQL types and typed documents
- thin service modules
- TanStack Query hooks as the only component-facing data layer

During migration, some older handwritten GraphQL documents and transport types may still exist. New work should follow the architecture guide instead of extending legacy patterns.
