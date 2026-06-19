using Microsoft.EntityFrameworkCore;
using TodoApi.Data;

namespace TodoApi.Jobs;

public class RefreshTokenCleanupJob
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(
        ApplicationDbContext db,
        ILogger<RefreshTokenCleanupJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Execute()
    {
        var expiredTokens =
            await _db.RefreshTokens
                .Where(x => x.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

        if (expiredTokens.Count == 0)
        {
            return;
        }

        _db.RefreshTokens.RemoveRange(expiredTokens);

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Hangfire deleted {Count} expired refresh tokens",
            expiredTokens.Count);
    }
}