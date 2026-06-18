using Microsoft.EntityFrameworkCore;
using TodoApi.Data;

namespace TodoApi.Services;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    public RefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<RefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            var expiredTokens =
                await db.RefreshTokens
                    .Where(x => x.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

            if (expiredTokens.Count > 0)
            {
                db.RefreshTokens.RemoveRange(expiredTokens);

                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Deleted {Count} expired refresh tokens", expiredTokens.Count);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}