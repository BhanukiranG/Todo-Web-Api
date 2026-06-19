# Hosting Grafana, Prometheus, and Jaeger on Render

If you want to move your monitoring stack from your local machine to the cloud, you cannot simply upload your `docker-compose.yml` to Render. Render's Web Services are designed to run exactly one Docker container per service. 

To host your full stack on Render, you must break them apart and deploy **three separate Web Services**. Here is the step-by-step guide.

---

## Step 1: Deploy Prometheus

Prometheus requires your custom `prometheus.yml` configuration file, which means you cannot use the default image straight from Docker Hub. You need to build a custom image.

1. Inside your `Monitoring` folder, create a file named `Dockerfile` with the following content:
   ```dockerfile
   FROM prom/prometheus
   COPY prometheus.yml /etc/prometheus/prometheus.yml
   ```
2. **Update `prometheus.yml`:** You can no longer scrape `host.docker.internal` because Prometheus is no longer running on your laptop. You must update it to scrape your live API on the internet:
   ```yaml
   global:
     scrape_interval: 5s

   scrape_configs:
     - job_name: 'todo-api'
       static_configs:
         - targets: ['todo-web-api-dnit.onrender.com'] # Your live API URL
   ```
3. Push this code to GitHub.
4. Go to the Render Dashboard -> **New** -> **Web Service**. Connect the repository and set the Root Directory to your `Monitoring` folder so Render builds this specific Dockerfile.

---

## Step 2: Deploy Grafana

Grafana does not need custom files to boot up, so you can deploy it instantly.

1. In the Render Dashboard, click **New** -> **Web Service**.
2. Scroll to the bottom and select **Deploy an existing image from a registry**.
3. Image URL: `grafana/grafana:latest`
4. Once Render boots it up, you will be given a public URL (e.g., `https://grafana-xyz.onrender.com`).
5. Open that URL, log in (default is `admin` / `admin`), go to **Data Sources**, and add Prometheus. 
6. For the connection URL, paste the public Render URL of your Prometheus server from Step 1.

---

## Step 3: Deploy Jaeger

Jaeger is also deployed directly from the registry.

1. In the Render Dashboard, click **New** -> **Web Service**.
2. Select **Deploy an existing image from a registry**.
3. Image URL: `jaegertracing/all-in-one:latest`
4. **The Critical Final Step:** Once Jaeger is live and has a public URL (e.g., `https://jaeger-xyz.onrender.com`), you must update your C# API so it knows where to send the traces. 
   Go into your `Program.cs` and change the OpenTelemetry configuration:
   ```csharp
   // CHANGE THIS:
   // options.Endpoint = new Uri("http://host.docker.internal:4317");
   
   // TO THIS:
   options.Endpoint = new Uri("https://jaeger-xyz.onrender.com:4317");
   ```
5. Push your C# code to GitHub so your API redeploys with the new Jaeger address.

---

## ⚠️ Critical Warning Regarding Costs

While your C# API runs perfectly on Render's free tier, Render strictly limits your free server hours per month and the amount of RAM you can use.

Grafana, Prometheus, and Jaeger are **very heavy** applications. Spinning up 3 brand new web services to run 24/7 will likely consume all of your free hours in a matter of days. Render will then pause your applications or ask for a credit card.

Because of this, most developers keep Grafana and Prometheus on their local laptop using `docker-compose up` while testing, or they use dedicated paid SaaS monitoring providers (like Datadog or New Relic) for production. Deploy this stack to Render only if you are willing to move to a paid tier!
