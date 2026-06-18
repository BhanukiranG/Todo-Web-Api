using System.Diagnostics;

namespace TodoApi.Telemetry;

public static class TelemetryConstants
{
    private const string ServiceName = "TodoApi";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
}