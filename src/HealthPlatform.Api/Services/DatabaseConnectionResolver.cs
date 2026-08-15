using Npgsql;

namespace HealthPlatform.Api.Services;

public static class DatabaseConnectionResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var host = configuration["Database:Host"];

        if (!string.IsNullOrWhiteSpace(host))
        {
            var port = int.TryParse(configuration["Database:Port"], out var parsedPort)
                ? parsedPort
                : 5432;

            var database = configuration["Database:Name"];
            var username = configuration["Database:User"];
            var password = configuration["Database:Password"];

            if (string.IsNullOrWhiteSpace(database) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Database:Host foi configurado, mas Database:Name/User/Password estao incompletos.");
            }

            return new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                Pooling = true,
                Timeout = 15,
                CommandTimeout = 30
            }.ConnectionString;
        }

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Configure ConnectionStrings:DefaultConnection ou Database:Host/Port/Name/User/Password.");
    }
}
