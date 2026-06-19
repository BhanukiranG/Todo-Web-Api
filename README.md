# Todo Web API

A modern, highly scalable, and fully observable Todo REST API built with **.NET 8**. This project demonstrates enterprise-grade backend practices including Clean Architecture, Distributed Caching, Background Processing, and Advanced Telemetry.

---

## 🚀 Tech Stack

*   **Framework:** .NET 8 Web API
*   **Architecture:** Clean Architecture (Domain, Application, Infrastructure, Presentation)
*   **Database:** PostgreSQL (Hosted on Neon in Production)
*   **ORM:** Entity Framework Core (with automated migrations)
*   **Caching:** Redis Distributed Cache (Hosted on Upstash in Production)
*   **Background Jobs:** Hangfire (using PostgreSQL as job storage)
*   **Authentication:** JWT (JSON Web Tokens) with Role-Based Access Control
*   **Observability:** OpenTelemetry, Serilog, Prometheus, Jaeger, Grafana
*   **Testing:** xUnit & Moq (100% Core Service Coverage)
*   **CI/CD:** GitHub Actions -> Automated deployment to Render

---

## 🏗️ Architecture

The project is structured following **Clean Architecture** principles to ensure separation of concerns:

1.  **`Domain/`**: The core of the application. Contains business entities (`TodoItem`, `User`) with zero external dependencies.
2.  **`Application/`**: Contains the core business logic (`TodoService`, `JwtService`), DTOs, and input validation (FluentValidation).
3.  **`Infrastructure/`**: Interacts with the outside world. Contains the EF Core `DbContext`, Data Repositories, and Background `Jobs`.
4.  **`Presentation/`**: The entry point. Contains API `Controllers`, global exception handling `Middleware`, and `Program.cs`.

---

## 🔌 Available APIs & Endpoints

### 📝 Todo Management
*   `GET /api/v1/todos` - Retrieve all todos (Served from Redis Cache if available)
*   `GET /api/v1/todos/{id}` - Retrieve a specific todo
*   `POST /api/v1/todos` - Create a new todo (Requires `Todos.Create` permission)
*   `PUT /api/v1/todos/{id}` - Update a todo
*   `DELETE /api/v1/todos/{id}` - Delete a todo (Requires `Todos.Delete` permission)

### 🔐 Authentication
*   `POST /api/v1/auth/register` - Register a new user account
*   `POST /api/v1/auth/login` - Authenticate and receive a JWT

### 🩺 System & Observability
*   `GET /swagger` - Interactive API Documentation
*   `GET /health` - Database connection and system health status
*   `GET /hangfire` - Interactive dashboard for background job processing
*   `GET /metrics` - Raw Prometheus metrics endpoint

---

## ⚙️ How to Run Locally

### Prerequisites
*   .NET 8 SDK
*   Docker Desktop (for local DB/Cache/Monitoring)

### 1. Start the Dependencies
This project uses Docker Compose to instantly spin up a local PostgreSQL database and Redis cache.
```bash
docker-compose up -d
```

### 2. Run the API
```bash
dotnet restore
dotnet build
dotnet run
```
The API will be available at `http://localhost:8080/swagger`.

### 3. Run the Monitoring Stack (Optional)
To view advanced OpenTelemetry metrics and traces:
```bash
cd Monitoring
docker-compose up -d
```
*   **Grafana:** `http://localhost:3000` (Visualize API performance metrics)
*   **Jaeger:** `http://localhost:16686` (Visualize distributed request traces)

---

## 🧪 Running Tests
The core application logic is fully tested using xUnit and Moq.
```bash
dotnet test
```

---

## 📚 Extensive Documentation
For deep dives into how this architecture functions under the hood, see the highly detailed guides included in the `Docs/` folder of this repository:
1. `Docs/deployment_architecture_guide.md`
2. `Docs/program_cs_explained.md`
3. `Docs/dotnet_foundations.md`
4. `Docs/configuration_files_explained.md`
5. `Docs/unit_testing_guide.md`
6. `Docs/local_monitoring_dashboard_guide.md`
