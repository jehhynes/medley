using Microsoft.Extensions.Hosting;

namespace Medley.Application;

public static class EnvironmentExtensions
{
    public static bool IsTesting(this IHostEnvironment environment)
    {
        return string.Equals(environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
    }
}

