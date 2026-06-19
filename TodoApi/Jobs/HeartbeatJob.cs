namespace TodoApi.Jobs;

public class HeartbeatJob
{
    private readonly ILogger<HeartbeatJob> _logger;

    public HeartbeatJob(
        ILogger<HeartbeatJob> logger)
    {
        _logger = logger;
    }

    public Task Execute()
    {
        _logger.LogInformation(
            "Hangfire heartbeat executed at {Time}",
            DateTime.UtcNow);

        return Task.CompletedTask;
    }
}