# Kape App migration

This Next.js financial application is being transformed into Kape App, a South African-focused personal finance experience.

## Current migration status

- Plaid and Dwolla configuration has been removed.
- Account transfers use a clearly labelled Appwrite demo flow and never move real money.
- Registration uses South African provinces, mobile numbers, four-digit postal codes, `DD/MM/YYYY` dates, ZAR and `en-ZA` conventions.
- Appwrite remains the temporary authentication and persistence platform.
- A provider-independent South African mock banking layer is the next data milestone.
- ASP.NET Core, SQL Server and Stitch integration remain later phases.

## Current stack

- Next.js
- TypeScript
- Appwrite
- React Hook Form
- Zod
- Tailwind CSS
- Chart.js
- shadcn/ui

## Local verification

```bash
npm install
npm run typecheck
npm run lint
npm run build
npm run dev
```

Before testing registration, apply the Appwrite changes documented in `docs/appwrite-south-african-registration-migration.md`. Keep real credentials in `.env.local`, never in tracked files.
