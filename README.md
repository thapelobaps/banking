# Kape App

Kape App is a South African-focused personal finance application with a Next.js frontend and an ASP.NET Core API backed by SQL Server.

## Current architecture

### Frontend

- Next.js 14
- TypeScript
- React Hook Form and Zod
- Tailwind CSS and shadcn/ui
- Server actions that call the Kape ASP.NET Core API
- HttpOnly access-token cookie managed by the Next.js server

### Backend

- .NET 10 ASP.NET Core minimal API
- ASP.NET Core Identity
- JWT authentication
- Entity Framework Core
- SQL Server
- Provider-independent South African demo banking service

Appwrite, Plaid and Dwolla are not used by the current application tree.

## Current product behaviour

- South African registration with all nine provinces
- `0` and `+27` mobile-number validation and normalisation
- Four-digit postal codes
- `DD/MM/YYYY` display with ISO date persistence
- ZAR and `en-ZA` formatting
- SQL-backed demo accounts and transactions
- Demo EFT transfers that never move real money
- Demo scenarios for Capitec, FNB, Absa, Standard Bank, Nedbank, TymeBank and Discovery Bank

Basic registration does not request an ID number, passport number, SSN or real bank credentials.

## Local setup

Read `docs/dotnet-sql-server-setup.md` before running the project.

Backend:

```powershell
$env:Jwt__SigningKey = "<generate-a-long-random-value>"
dotnet restore backend/Kape.Api/Kape.Api.csproj
dotnet ef database update --project backend/Kape.Api --startup-project backend/Kape.Api
dotnet run --project backend/Kape.Api
```

Frontend:

```powershell
Copy-Item .env.example .env.local
npm install
npm run typecheck
npm run lint
npm run build
npm run dev
```

## Security note

A credential was committed before this migration. Removing Appwrite from the current tree does not remove that credential from Git history. Revoke it before any further use and review history cleanup separately; do not restore the deleted tracked `.env` file.

## Later roadmap

- Complete automated tests and CI
- Continue the Kape App visual rebrand and minimal brown, white and grey design system
- Expand Overview, Accounts and Transactions
- Add secure KYC only when required
- Add Stitch near the end through the banking provider abstraction
