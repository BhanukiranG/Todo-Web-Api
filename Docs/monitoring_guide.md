# How Monitoring Works in Your .NET App

When an application goes live, you need to know exactly what is happening inside it. If it crashes, or if it suddenly gets very slow, you need data to figure out why. 

Your application uses the "Three Pillars of Observability" to monitor its health: **Logging**, **Tracing**, and **Metrics**. Here is how they are configured in your code.

---

## 1. Logging (The "What Happened" Pillar)
**Tool you are using:** `Serilog`

By default, .NET prints simple text to the console. You replaced this with **Serilog**, which does "Structured Logging." 
Instead of just printing a sentence, Serilog saves your logs as data objects (like JSON). This makes it incredibly easy to search through millions of logs later.

**Where it happens in your code:**
*   In `Program.cs`, you configured Serilog to write to the Console AND to a rolling text file (`logs/log-.txt`).
*   You added `app.UseSerilogRequestLogging()`. This automatically logs exactly how many milliseconds every single web request takes to process.
*   In `TodoService.cs`, you are manually logging important events: `_logger.LogInformation("Creating todo with title {Title}", request.Title);`

---

## 2. Health Checks (The "Are You Alive" Pillar)
**Tool you are using:** `Microsoft.Extensions.Diagnostics.HealthChecks`

When you host your app on the cloud (like Render or AWS), the cloud provider needs to know if your app has crashed so it can restart it. 

**Where it happens in your code:**
*   In `Program.cs`, you added `builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();`
*   You mapped this to the URL `/health`.
*   Now, if Render visits `https://todo-web-api-dnit.onrender.com/health`, your app doesn't just reply "I'm alive"—it actually runs a quick check to see if the PostgreSQL database is connected. If the database goes down, your app reports "Unhealthy!"

---

## 3. Tracing & Metrics (The "Where is the Bottleneck" Pillar)
**Tool you are using:** `OpenTelemetry`

This is the most advanced and impressive part of your setup. OpenTelemetry is the industry standard for monitoring massive cloud systems. 

### A. Metrics (The Dashboard Numbers)
Metrics are numbers that go up and down over time (e.g., "CPU usage", "Requests per second").
*   In `Program.cs`, you added `.AddPrometheusExporter()` and `app.MapPrometheusScrapingEndpoint()`.
*   This exposes a hidden URL on your server (usually `/metrics`). A tool called **Prometheus** can visit this URL every 5 seconds to scrape your numbers and draw beautiful graphs (usually in a dashboard tool called Grafana).

### B. Distributed Tracing (Following the Request)
Tracing allows you to follow a single web request as it jumps between your API, your Database, and your Redis cache. It creates a "waterfall" graph showing exactly where time was spent.
*   In `TodoService.cs`, you wrote this amazing code:
    ```csharp
    using var activity = TelemetryConstants.ActivitySource.StartActivity("GetAllTodos");
    activity?.SetTag("cache.enabled", true);
    ```
*   This creates a custom "Span." When you look at your traces, you will explicitly see a block called "GetAllTodos" and you can see exactly how many milliseconds it took to fetch from the cache vs the database!

### How to visualize OpenTelemetry?
OpenTelemetry doesn't have a UI by itself. It just sends data. In your `Program.cs`, you are sending data to `http://host.docker.internal:4317`. 
To actually *see* these beautiful graphs locally, you would typically run a tool like **Jaeger** or the **.NET Aspire Dashboard** via Docker, which catches the data on port 4317 and draws the graphs for you.
