# Understanding `Program.cs`: The Heart of Your .NET App

The `Program.cs` file is the entry point of your entire application. When you run your app, this is the very first file that executes.

In modern .NET (version 6 and above), `Program.cs` is divided into **two strict sections**:
1. **The Builder Phase (Dependency Injection):** Where you tell the app *what tools* it has available (Database, Cache, Authentication).
2. **The Application Phase (Middleware Pipeline):** Where you define *how a web request travels* through your app.

Here is a detailed breakdown of your `Program.cs` file.

---

## Part 1: The Builder Phase (Services & Configuration)

*Rule: Everything in this section MUST be placed before `builder.Build()`.*

```csharp
// 1. INITIALIZE SERILOG EARLY
// We set up Serilog before anything else so that if the app crashes while booting up, 
// we still capture the error in the console.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// 2. CREATE THE BUILDER
// This object collects all the configurations, settings, and services your app needs.
var builder = WebApplication.CreateBuilder(args);

// 3. OPENTELEMETRY (Metrics & Tracing)
// This adds advanced monitoring so you can see exactly how long database queries 
// and HTTP requests take (often visualized in tools like Prometheus or Jaeger).
builder.Services.AddOpenTelemetry()
    // ... tracing and metrics configuration ...

// 4. OVERRIDE DEFAULT LOGGING
// Tells the builder to stop using default Microsoft logging and use Serilog instead.
builder.Host.UseSerilog((context, configuration) => { ... });

// 5. ENABLE CONTROLLERS
// Tells .NET to look for files ending in "Controller" and treat them as API endpoints.
builder.Services.AddControllers();

// 6. SETUP CACHING
// AddMemoryCache allows standard RAM caching.
builder.Services.AddMemoryCache();
// AddStackExchangeRedisCache connects your app to Redis for distributed cloud caching.
builder.Services.AddStackExchangeRedisCache(options => { ... });

// 7. API VERSIONING
// Allows you to have "v1" and "v2" of your API running at the same time so you 
// don't break older mobile apps when you upgrade your server.
builder.Services.AddApiVersioning(...);

// 8. AUTHENTICATION & AUTHORIZATION
// Tells .NET how to validate JWT tokens. If a user sends a token that wasn't signed 
// by your "Jwt:Key", .NET will instantly reject the request with a 401 Unauthorized.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...);

// Defines strict policies. For example, to trigger the "TodoCreatePolicy", 
// the user's token MUST contain a claim saying "Permission: Todos.Create".
builder.Services.AddAuthorization(options => { ... });

// 9. HANGFIRE (Background Jobs)
// Sets up Hangfire and tells it to save its job queues in your PostgreSQL database.
builder.Services.AddHangfire(...);
builder.Services.AddHangfireServer();

// 10. DATABASE CONNECTION
// Connects Entity Framework Core to your PostgreSQL database.
builder.Services.AddDbContext<ApplicationDbContext>(...);

// 11. DEPENDENCY INJECTION (The Most Important Rule!)
// AddScoped: A new instance of this class is created for EVERY web request.
// AddTransient: A brand new instance is created EVERY SINGLE TIME a class asks for it.
// AddSingleton: Only ONE instance is created when the app boots and is shared by everyone.
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddTransient<RefreshTokenCleanupJob>();

// 12. VALIDATION
// Tells .NET to automatically run your FluentValidators before letting bad data hit your controllers.
builder.Services.AddFluentValidationAutoValidation();

// 13. HEALTH CHECKS
// Adds a system to verify the database is online.
var healthChecks = builder.Services.AddHealthChecks();

// 14. SWAGGER DOCUMENTATION
// Reads your code and automatically builds the beautiful API documentation UI.
builder.Services.AddSwaggerGen(...);

// 15. RATE LIMITING
// Protects your API from DDoS attacks or spam. 
// Example: The "login" rate limiter only allows 5 requests per minute per user!
builder.Services.AddRateLimiter(...);
```

---

## The Transition

```csharp
// The Builder Phase is over! We lock in all the configurations above and build the final Application object.
var app = builder.Build();
```

---

## Part 2: The Middleware Pipeline

*Rule: The ORDER of these lines is incredibly important. A web request hits the top line first and travels down through the pipeline. If you put Authentication below Authorization, your app will crash because it will try to check user permissions before it even figures out who the user is!*

```csharp
// 1. SWAGGER UI
// Creates the webpage at /swagger where you test your API.
app.UseSwagger();
app.UseSwaggerUI(...);

// 2. EXCEPTION HANDLING
// A custom middleware. If your app crashes anywhere below this line, this catches it 
// and returns a nice JSON error instead of an ugly HTML crash page.
app.UseMiddleware<ExceptionMiddleware>();

// 3. LOGGING
// Attaches a unique "Correlation ID" to the request so you can track it in the logs.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

// 4. SECURITY TRANSLATION
// Forces HTTP requests to upgrade to secure HTTPS.
app.UseHttpsRedirection();

// 5. RATE LIMITING (Must be before Auth!)
// Checks if the user is spamming the API. If they are, it stops the request here.
app.UseRateLimiter();

// 6. AUTHENTICATION (Who are you?)
// Reads the JWT token from the headers to figure out who the user is.
app.UseAuthentication();

// 7. AUTHORIZATION (Are you allowed here?)
// Checks the user's role to see if they have permission to access the specific controller.
app.UseAuthorization();

// 8. HANGFIRE DASHBOARD
// Creates the webpage at /hangfire to view background jobs.
app.UseHangfireDashboard(...);

// 9. ROUTING 
// The request finally reaches your Controllers (where your actual business logic lives).
app.MapControllers();

// 10. HEALTH ENDPOINT
// Maps the /health URL so cloud providers can check if your app is alive.
app.MapHealthChecks("/health", ...);

// 11. AUTOMATIC DATABASE MIGRATIONS
// A clever trick! Every time the app boots up, it checks if the database is missing any tables.
// If it is, it automatically creates them before the app finishes starting.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// 12. BACKGROUND JOB SCHEDULING
// Tells Hangfire to run the RefreshTokenCleanupJob exactly once every hour.
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<RefreshTokenCleanupJob>(...);
}

// 13. START THE SERVER
// The app is fully configured and now listens for incoming web traffic indefinitely!
app.Run();
```
