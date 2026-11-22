using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskApi.Api.Health;

public class MemoryHealthCheck(IConfiguration configuration, ILogger<MemoryHealthCheck> logger) : IHealthCheck
{
    public const int DefaultLimitMB = 512;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var limitMB = configuration.GetValue("HealthChecks:MemoryLimitMB", DefaultLimitMB);
        var usedMB = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);
        var data = new Dictionary<string, object> { ["usedMB"] = usedMB, ["limitMB"] = limitMB };

        if (usedMB >= limitMB)
        {
            logger.LogError("Uso de memória {UsedMB} MB atingiu o limite de {LimitMB} MB", usedMB, limitMB);
            return Task.FromResult(HealthCheckResult.Unhealthy($"Memória acima do limite ({usedMB}/{limitMB} MB)", data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"Memória OK ({usedMB}/{limitMB} MB)", data));
    }
}
