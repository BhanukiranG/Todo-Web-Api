# Technical Documentation: Todo Web API

## 1. Project Overview
The **Todo Web API** is a modern, enterprise-grade RESTful API built with **.NET 8**. It provides a highly scalable and fully observable backend for managing Todo items, alongside robust authentication and background processing capabilities. 

## 2. Architecture & Design Patterns
The project strictly follows **Clean Architecture** principles to ensure separation of concerns, testability, and maintainability.

### 2.1 Layers
* **Domain (`TodoApi/Domain/`)**: The core of the application. Contains business entities and models (e.g., `TodoItem`, `User`). This layer has zero external dependencies.
* **Application (`TodoApi/Application/`)**: Contains the core business logic. 
  * `Services`: Interfaces and implementations for business operations (`TodoService`, `JwtService`).
  * `DTOs`: Data Transfer Objects for API requests and responses.
  * `Validators`: Input validation logic using FluentValidation (`CreateTodoRequestValidator`).
* **Infrastructure (`TodoApi/Infrastructure/`)**: Interacts with the outside world.
  * `Data`: Contains the Entity Framework Core `ApplicationDbContext`.
  * `Repositories`: Implementations of data access interfaces (`TodoRepository`).
  * `Jobs`: Background jobs powered by Hangfire (`RefreshTokenCleanupJob`, `HeartbeatJob`).
* **Presentation (`TodoApi/Presentation/`)**: The entry point of the application.
  * `Controllers`: API endpoints.
  * `Middleware`: Global exception handling, request logging, and correlation ID tracking.

### 2.2 Key Technologies & Frameworks
* **Framework**: .NET 8 Web API
* **Database**: PostgreSQL (Entity Framework Core)
* **Caching**: Redis Distributed Cache (`AddStackExchangeRedisCache`)
* **Background Jobs**: Hangfire (using PostgreSQL for job storage)
* **Authentication/Authorization**: JWT Bearer Tokens with Role-Based Access Control (Policies like `TodoReadPolicy`, `TodoCreatePolicy`)
* **Validation**: FluentValidation
* **API Versioning**: `Asp.Versioning` (v1 and v2 configured)
* **Rate Limiting**: Fixed window rate limiting applied (`default` and `login` policies)
* **Testing**: xUnit & Moq

## 3. Data & Storage
### 3.1 Database
* The application uses **PostgreSQL**.
* Database interactions are managed via **Entity Framework Core**.
* Automated migrations are executed on startup (`db.Database.Migrate()`).

### 3.2 Caching
* **Redis** is used as a distributed cache to store frequently accessed data and reduce database load, particularly for retrieving lists of todos.

## 4. Observability & Monitoring
The application is heavily instrumented for observability:
* **OpenTelemetry**: Integrated for distributed tracing and metrics collection. Exports telemetry to an OTLP endpoint (Jaeger/Grafana setup via `http://host.docker.internal:4317`).
* **Serilog**: Replaces the default .NET logger. Configured to output structured logs to the console and daily rolling text files (`logs/log-.txt`). Includes context like `CorrelationId`.
* **Prometheus**: Exposes a scraping endpoint (`/metrics`) for raw performance metrics.
* **Health Checks**: Available at `/health` to verify database connectivity and overall system health.

## 5. Security
* **JWT Authentication**: Endpoints are secured using JWTs. `JwtService` handles token generation and validation.
* **Authorization Policies**: Role-based access control is enforced via Claims (e.g., `Todos.Read`, `Todos.Create`, `Todos.Delete`).
* **Rate Limiting**: Prevents abuse by limiting the number of requests per window.

## 6. Background Processing
* **Hangfire** is integrated for robust background job processing, using PostgreSQL as its persistent storage.
* Includes recurring jobs like `RefreshTokenCleanupJob` (runs hourly) and `HeartbeatJob`.
* A Hangfire Dashboard is available at `/hangfire`.

## 7. Configuration & Environments
* Configuration is managed via `appsettings.json` and environment-specific overrides (e.g., `appsettings.Development.json`).
* Uses Docker and Docker Compose (`docker-compose.yml`) to orchestrate the application along with its dependencies (PostgreSQL, Redis) and the monitoring stack (Grafana, Jaeger, Prometheus) located in the `Monitoring/` directory.

## 8. Deployment & CI/CD
* Containerized using a `Dockerfile`.
* CI/CD pipelines are set up via **GitHub Actions** (`.github/` directory) for automated testing and deployment (e.g., to Render).

## 9. API Documentation
* Interactive API documentation is available via **Swagger** (`/swagger`). It supports multiple API versions (v1, v2) and includes UI support for JWT Bearer token authentication.

## 10. File Structure Summary
```text
Todo/
├── Docs/                     # Detailed architectural guides and documentation
├── Monitoring/               # Docker Compose setup for observability stack (Grafana, Jaeger)
├── TodoApi/                  # Main .NET 8 Web API Project
│   ├── Application/          # Business Logic, DTOs, Validators
│   ├── Domain/               # Core Entities
│   ├── Infrastructure/       # Data Access, Hangfire Jobs, EF Core
│   ├── Presentation/         # Controllers, Middleware
│   ├── Migrations/           # EF Core Database Migrations
│   ├── Telemetry/            # Custom Telemetry configurations
│   ├── Program.cs            # App configuration and DI container setup
│   ├── appsettings.json      # App Configuration
│   └── docker-compose.yml    # Local dependencies (DB, Cache)
├── TodoApi.UnitTests/        # xUnit Test Project
├── Todo.sln                  # Solution File
└── README.md                 # Quick start guide
```
