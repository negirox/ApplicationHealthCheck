using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationHealthCheck.HealthChecks
{
    public class APIHealthChecks : IHealthCheck
    {
        private Random _random = new Random();
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var responseTime = _random.Next(1, 300);
            if (responseTime < 100)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Healthy result from MyHealthCheck"));
            }
            else if (responseTime < 200)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Degraded result from MyHealthCheck"));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("Unhealthy result from MyHealthCheck"));
        }
    }
}
