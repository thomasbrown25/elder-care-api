using elder_care_api.Data;
using elder_care_api.DbLogger;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace elder_care_api.Services.HealthService
{
    public class HealthService(ILogging logging, IConfiguration configuration) : IHealthCheck
    {
        private readonly ILogging _logging = logging;
        private readonly IConfiguration _configuration = configuration;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            _logging.LogTrace("Health Check Succeeded");

            return HealthCheckResult.Healthy("A healthy result.");
        }

    }
}