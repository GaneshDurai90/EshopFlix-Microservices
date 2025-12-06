# EshopFlix Microservices

EshopFlix is an e-commerce application built with .NET microservices and clean design patterns. This repository hosts multiple backend and frontend services, fronted by an API gateway.

> Status: WIP. This README documents the project layout, how to build locally, and a roadmap for production-grade features.

## Repository layout

```
.
├─ ApiGateways/
├─ BackendServices/
├─ FrontendServices/
├─ MicroservicesApp.sln
├─ .dockerignore
├─ .gitattributes
└─ .gitignore
```

- [ApiGateways/](./ApiGateways): Edge routing, versioning, and auth policies. (e.g., Ocelot or YARP)
- [BackendServices/](./BackendServices): Core domain services (e.g., Catalog, Basket/Cart, Ordering, Identity, Payment).
- [FrontendServices/](./FrontendServices): End-user web UI or BFFs (Razor/MVC/SPA).
- [MicroservicesApp.sln](./MicroservicesApp.sln): Solution file referencing all projects.

If service directories are empty or placeholders, they’re planned for future commits.

## Tech stack (used or planned)

- .NET (ASP.NET Core), C#
- Entity Framework Core (per-service DB and migrations)
- API Gateway (Ocelot or YARP)
- Message broker for async events (RabbitMQ or Kafka)
- SQL database per service (SQL Server or PostgreSQL)
- Redis for caching/session (optional)
- OpenAPI/Swagger per service
- Observability with Health Checks and OpenTelemetry (Jaeger/Zipkin, Prometheus)
- Resilience with Polly (timeouts, retries, circuit breakers)

## Quick start

Prerequisites:
- .NET SDK 8.0+ installed
- A running database or container (per service) if required by the service you run
- Node.js (only if a frontend SPA exists)

1) Clone and restore
```bash
git clone https://github.com/GaneshDurai90/EshopFlix-Microservices.git
cd EshopFlix-Microservices
dotnet restore MicroservicesApp.sln
```

2) Build
```bash
dotnet build MicroservicesApp.sln -c Release
```

3) Run a service
- Replace with an actual service project path once services are added:
```bash
dotnet run -c Release -p BackendServices/<ServiceName>/<ServiceName>.csproj
```
- Swagger UI (when enabled) is typically at:
  - https://localhost:PORT/swagger

4) API Gateway (when configured)
- Run gateway project (e.g., Ocelot/YARP) from [ApiGateways/](./ApiGateways) and hit the gateway endpoint instead of per-service URLs.

## Configuration

Use environment variables for configuration:
- ASPNETCORE_ENVIRONMENT=Development
- ConnectionStrings__Default=...
- MessageBroker__Host=...
- Redis__ConnectionString=...
- Jwt__Authority=..., Jwt__Audience=...

Local secrets:
- For development, consider `dotnet user-secrets` per service.
- Never commit secrets.

## Database and migrations

Each service owns its database and EF Core migrations:
```bash
# From a service project directory
dotnet ef migrations add InitialCreate
dotnet ef database update
```
Document connection strings and seeding scripts per service.

## Testing

- Unit tests: domain/application layers
- Integration tests: WebApplicationFactory + Testcontainers (SQL/Redis/RabbitMQ)
- Contract tests: consumer/provider (e.g., Pact)
- End-to-end: core checkout flow (Playwright) when UI exists

Run tests:
```bash
dotnet test MicroservicesApp.sln --collect:"XPlat Code Coverage"
```

## Observability

- Health checks (liveness/readiness): `/health` endpoints per service
- OpenTelemetry (recommended):
  - Traces to Jaeger/Zipkin
  - Metrics to Prometheus
- Correlation IDs: propagate across gateway/services for trace continuity

## Resilience and reliability

- Apply Polly policies per outbound dependency:
  - Timeouts, retries (jittered backoff), circuit breakers, bulkheads
- Idempotency for commands (checkout/payment)
- Outbox pattern for reliable event publishing
- Sagas/process managers for long-running workflows (order lifecycle)

## Local development with containers (optional)

If you plan to add Docker and docker-compose:
- Add Dockerfiles per service with multi-stage builds
- Add `docker-compose.yml` to orchestrate services + infra (SQL/Redis/RabbitMQ/Jaeger)
- Developer container (devcontainer) for consistent dev environment

## Roadmap

- [ ] Define service list and ownership (Catalog, Basket, Ordering, Identity, Payment, Inventory)
- [ ] Add API Gateway configuration with versioned routes and edge auth
- [ ] Implement OpenAPI per service and generate typed clients
- [ ] Introduce message broker and Outbox pattern
- [ ] Enable OpenTelemetry tracing and standard health checks
- [ ] Add CI (build/test/coverage), CodeQL, and Dependabot
- [ ] Add docker-compose for local orchestration
- [ ] Add integration and contract tests
- [ ] Harden security (JWT scopes/roles, secret management, data protection)

## Contributing

- Use feature branches and PRs
- Keep services independent with clear APIs and versioning
- Add/update EF migrations with each schema change
- Write tests for new features and bug fixes
- 
[![.NET 9 CI (All Services)](https://github.com/GaneshDurai90/EshopFlix-Microservices/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/GaneshDurai90/EshopFlix-Microservices/actions/workflows/dotnet-ci.yml)

## License

Copyright (c) 2025 Your Name

All rights reserved.

This repository and its contents are provided for viewing and
personal educational purposes only.

Permission is NOT granted to:
- Use this code in any personal, academic, or commercial project
- Modify, copy, or create derivative works from this code
- Distribute, sublicense, publish, or sell this code
- Include this code in any software, application, or service

Permission IS granted only to:
- View the source code for personal learning purposes

Any use of this code beyond the permissions explicitly granted
above is strictly prohibited.
