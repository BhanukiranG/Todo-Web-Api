# Configuration Files Explained: A Line-by-Line Guide

As a developer, you will spend a lot of time configuring your application. This document breaks down the three most important configuration files in your project, explaining exactly what the code inside them is doing.

---

## 1. `appsettings.json` (Your .NET Configuration)

This file is the "control panel" for your .NET application. Whenever your C# code needs a setting (like a password, a database URL, or a logging rule), it looks here first.

Here is what your file is doing:

```json
{
  // 1. STANDARD LOGGING
  // This tells .NET how much information to print to the console. 
  // "Warning" means it will hide standard info from Microsoft libraries so your console doesn't get spammed.
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  
  // 2. SECURITY
  // This tells .NET which domain names are allowed to connect to your API. 
  // "*" means any domain is allowed (which is fine for APIs).
  "AllowedHosts": "*",
  
  // 3. DATABASE CONNECTION
  // This is where your app looks for PostgreSQL. 
  // Notice it says "Host=localhost". This is why it only works on your local machine!
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=tododb;Username=admin;Password=admin123"
  },
  
  // 4. AUTHENTICATION (JSON Web Tokens)
  // When a user logs in, you generate a JWT token. 
  // The "Key" is the secret password used to digitally sign the token so hackers can't forge them.
  "Jwt": {
    "Key": "SuperSecretKeyForJwtAuthentication123456",
    "Issuer": "TodoApi",
    "Audience": "TodoApiUsers"
  },
  
  // 5. CACHING
  // Where to find your local Redis server.
  "Redis": {
    "ConnectionString": "redis:6379"
  },
  
  // 6. SERILOG (Advanced Logging)
  // You are using an advanced library called Serilog to save your logs to actual files.
  // This section tells Serilog to write logs to the console AND to a file called "logs/log-.txt".
  // "rollingInterval": "Day" means it creates a brand new log file every single day!
  "Serilog": { ... }
}
```

**Crucial Note on Cloud Hosting:** When you deploy to Render, you *do not* change this file. Instead, you create **Environment Variables** in Render (like `ConnectionStrings__DefaultConnection`). .NET is smart enough to see the environment variable in Render and completely ignore what is written in this `appsettings.json` file.

---

## 2. `docker-compose.yml` (Your Local Server Orchestrator)

Running PostgreSQL and Redis manually on Windows is annoying. `docker-compose.yml` is a script that tells Docker to instantly boot up "mini-computers" (containers) running exactly what you need for local development.

```yaml
services:
  # CONTAINER 1: THE DATABASE
  postgres:
    image: postgres:16 # Downloads the official Postgres version 16 from the internet
    container_name: todo-postgres
    environment: # Creates the default database and password instantly
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: admin123
      POSTGRES_DB: tododb
    ports:
      - "5432:5432" # Maps the container's port to your Windows laptop's port
    volumes:
      - postgres_data:/var/lib/postgresql/data # MAGIC: Saves your data so if you stop Docker, your Todo items aren't deleted!

  # CONTAINER 2: THE CACHE
  redis:
    image: redis:7 # Downloads the official Redis version 7
    container_name: todo-redis
    ports:
      - "6379:6379"

  # CONTAINER 3: YOUR C# API
  api:
    build:
      context: .
      dockerfile: Dockerfile # Tells Docker to look at your Dockerfile to build your C# code
    container_name: todo-api
    depends_on:
      - postgres # Tells your API to wait until the database is fully booted up before starting!
    ports:
      - "8080:8080"
    environment:
      # We inject environment variables here so the API container knows how to talk to the Postgres container
      ASPNETCORE_URLS: http://0.0.0.0:8080
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=tododb;Username=admin;Password=admin123

# Declares the permanent storage hard drive used by Postgres above
volumes:
  postgres_data:
```

---

## 3. `ci.yml` (Your GitHub Actions Automation)

This file lives in `.github/workflows/ci.yml`. It is a script that GitHub runs completely automatically on their own servers whenever you push code. This is called **Continuous Integration & Continuous Deployment (CI/CD)**.

```yaml
name: .NET CI/CD Pipeline

# 1. THE TRIGGER
# Tells GitHub: "Only run this script when code is pushed to the 'main' branch."
on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]

jobs:
  # 2. JOB 1: THE TESTER (Continuous Integration)
  build_and_test:
    name: Build and Test
    runs-on: ubuntu-latest # GitHub boots up a free, temporary Linux computer for you

    steps:
    - name: Checkout Code
      uses: actions/checkout@v4 # Downloads your latest code onto the GitHub computer

    - name: Setup .NET 8
      uses: actions/setup-dotnet@v4 # Installs the C# compiler on the GitHub computer
      with:
        dotnet-version: 8.0.x

    - name: Restore dependencies
      run: dotnet restore Todo.sln # Downloads all your NuGet packages

    - name: Build
      run: dotnet build Todo.sln --configuration Release --no-restore # Compiles your code

    - name: Test
      # Runs your TodoApi.UnitTests. 
      # IF ANY TEST FAILS, THE SCRIPT STOPS HERE! Your broken code will never reach Render.
      run: dotnet test Todo.sln --configuration Release --no-build --verbosity normal 

  # 3. JOB 2: THE DEPLOYER (Continuous Deployment)
  deploy:
    name: Deploy to Render
    needs: build_and_test # Tells GitHub: "Do not run this unless 'build_and_test' succeeds!"
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' && github.event_name == 'push' # Only deploy if pushing to main

    steps:
    - name: Trigger Render Deployment
      env:
        # Grabs the secret Render Webhook URL you saved in your GitHub settings
        deploy_url: ${{ secrets.RENDER_DEPLOY_HOOK }} 
      run: |
        # This sends a "ping" (an HTTP POST request) to Render. 
        # It's basically saying: "Hey Render, the tests passed! Go download the new code and restart the server!"
        echo "Triggering Render deployment..."
        curl -X POST "$deploy_url"
```
