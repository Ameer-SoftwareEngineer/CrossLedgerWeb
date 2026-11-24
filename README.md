# CrossLedgerWeb

Cross-border multi-currency wallet & remittance platform — an ASP.NET Core Web API backend, a Blazor WebAssembly frontend, a SQL Server ledger, and a full Azure deployment.

CrossLedgerWeb is not a CRUD demo. It models the problems real payment infrastructure has to solve: a double-entry ledger where balances are derived rather than stored, FX quote locking, idempotent transfers, optimistic concurrency, multi-provider payment routing with audited failover, and step-up authentication on sensitive operations.

The full design rationale, business rules, data access strategy, provider orchestration, security model and delivery roadmap are documented in [`CrossLedger_Specification-2.pdf`](CrossLedger_Specification-2.pdf) — read that first.

## Solution structure

```
CrossLedgerWeb.sln
src/
  CrossLedgerWeb.Domain            entities, value objects, domain events — no external dependencies
  CrossLedgerWeb.Application       CQRS handlers, interfaces, validators — depends on Domain
  CrossLedgerWeb.Infrastructure    EF Core, Redis, Dapper, email — depends on Application, Domain
  CrossLedgerWeb.Providers         payment/FX provider implementations behind shared interfaces
  CrossLedgerWeb.Api               controllers, middleware, DI wiring — depends on Application, Infrastructure
  CrossLedgerWeb.Client            Blazor WebAssembly front end
  CrossLedgerWeb.Shared            DTOs and validators shared by Api + Client
tests/
  CrossLedgerWeb.Domain.Tests          pure unit tests, no mocks
  CrossLedgerWeb.Application.Tests     unit tests with Moq
  CrossLedgerWeb.Integration.Tests     WebApplicationFactory + Testcontainers
db/
  procedures/*.sql              source of record for every dbo.usp_* stored procedure;
                                 applied via an EF Core migration (CREATE OR ALTER, so it's
                                 safe whether the object already exists or not)
infra/
  docker-compose.yml            local API + SQL Server
  main.bicep                    Azure environment as code (never deployed from here - see below)
```

Dependencies point inward only (Clean Architecture): `Domain` has no references, and outer layers depend on abstractions defined further in.

## Getting started

```bash
dotnet restore
dotnet build
dotnet test
```

Local infrastructure (SQL Server, Redis):

```bash
docker compose -f infra/docker-compose.yml up -d
```

## Status

Phase 1 (ledger, FX, provider routing, security, Blazor front end) and Phase 2 (Stripe Issuing card issuance, real-time authorization, spending controls, PIN management, physical fulfillment and reissue) are both built and under CI. `infra/main.bicep` is authored and syntax-validated (`az bicep build`) but has never been deployed — that needs the maintainer's own Azure subscription.

## Branching

`main` is the protected, always-buildable branch. Work happens on `feature/*` branches merged back via pull request.
