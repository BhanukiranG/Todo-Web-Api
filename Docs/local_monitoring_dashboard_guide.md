# Your Local Monitoring Stack (Grafana, Prometheus, & Jaeger)

You have set up an incredibly advanced, enterprise-grade monitoring system in your `Monitoring` folder. This guide explains exactly how these Docker containers talk to your C# API and how you can view the data.

---

## 1. How the Architecture Connects

When your `.NET` API runs, it is generating a massive amount of data (metrics and traces) thanks to OpenTelemetry. However, the API doesn't store this data—it needs somewhere to send it.

That is where your `Monitoring/docker-compose.yml` comes in. It spins up three separate systems:

### 1. Jaeger (The Tracer)
*   **What it does:** Jaeger tracks the "waterfall" of your code. It tells you exactly how many milliseconds were spent in the database versus the Redis cache for every single API request.
*   **How it connects:** In your C# `Program.cs`, you explicitly told OpenTelemetry to push traces to `http://host.docker.internal:4317`. Jaeger is listening on port 4317, so it catches all the data your app throws at it.
*   **How to view it:** Open your browser and go to **`http://localhost:16686`**. You can search for specific API endpoints and see a beautiful timeline of how the code executed.

### 2. Prometheus (The Metrics Engine)
*   **What it does:** Prometheus is a time-series database. It stores raw numbers over time (like CPU usage, memory, or how many HTTP requests happen per second).
*   **How it connects:** Unlike Jaeger (where the API pushes data), Prometheus *pulls* data. In your `prometheus.yml` file, you told it to scrape `host.docker.internal:8080`. Every 5 seconds, Prometheus reaches into your .NET app's hidden `/metrics` endpoint and downloads the latest numbers.
*   **How to view it:** Go to **`http://localhost:9090`**. This is the raw data engine. You can query it, but the graphs here are very basic.

### 3. Grafana (The Dashboard)
*   **What it does:** Grafana makes Prometheus's raw, ugly data look beautiful.
*   **How it connects:** Grafana doesn't talk to your API directly. Instead, Grafana talks to Prometheus. 
*   **How to view it:** Go to **`http://localhost:3000`** (the default login is usually `admin` for both username and password). 

---

## 2. How to Run and View Your Dashboards Locally

Follow these exact steps to see your monitoring stack in action:

1. **Start your C# API** locally in Visual Studio/Rider, or by running `dotnet run` in your terminal. Ensure it is running on port 8080.
2. Open a terminal and navigate to your `Monitoring` folder.
3. Run this command to boot up the monitoring stack in the background:
   ```bash
   docker-compose up -d
   ```
4. Open Swagger (`http://localhost:8080/swagger`) and click around to generate some traffic (create some todos, fetch them, etc.).
5. **Connect Grafana to Prometheus:**
   * Open `http://localhost:3000` and log in.
   * Go to **Connections > Data Sources** and add a new source.
   * Select **Prometheus**.
   * In the URL box, type `http://prometheus:9090` (because Grafana is inside Docker, it can use the container name to find it).
   * Click **Save & Test**. 
6. Now you can create a dashboard in Grafana to see your live API metrics, and open `http://localhost:16686` to trace your requests in Jaeger!
