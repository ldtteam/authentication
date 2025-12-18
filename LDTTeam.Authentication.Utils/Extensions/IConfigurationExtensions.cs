using Microsoft.Extensions.Configuration;

namespace LDTTeam.Authentication.Utils.Extensions;

// ReSharper disable once InconsistentNaming
public static class IConfigurationExtensions
{
    /// <summary>
    /// Extension method which creates a connection string from configured values.
    /// </summary>
    /// <param name="configuration">The configuration to look up the connection data in.</param>
    /// <param name="databaseName">The name of the database to use.</param>
    /// <returns>The connection string.</returns>
    public static string CreateConnectionString(this IConfiguration configuration, string databaseName)
    {
        var databaseSection = configuration.GetSection("Database");
        
        var server = databaseSection["Server"] ?? "localhost";
        var port = databaseSection["Port"] ?? "5432";
        var username = databaseSection["Username"] ?? "postgres";
        var password = databaseSection["Password"] ?? "postgres";
        var timeout = databaseSection["Timeout"] ?? "600";
        
        var uris = databaseSection.GetSection("Uris");
        
        Console.WriteLine("Using database connection settings:");
        foreach (var (key, value) in uris.AsEnumerable())
        {
            Console.WriteLine("  - {0}: {1}", key, value);
        }
        
        var uri = uris[databaseName];
        if (!string.IsNullOrEmpty(uri))
        {
            var parsedUri = new Uri(uri);
            server = parsedUri.Host;
            port = parsedUri.Port.ToString();
            username = parsedUri.UserInfo.Split(':')[0];
            password = parsedUri.UserInfo.Split(':')[1];
            databaseName = parsedUri.LocalPath;
            
            if (databaseName.StartsWith("/"))
                // Remove leading slash
                databaseName = databaseName[1..];
        }

        return $"Server={server};Port={port};User Id={username};Password={password};Database={databaseName};Timeout={timeout};";
    }
}
