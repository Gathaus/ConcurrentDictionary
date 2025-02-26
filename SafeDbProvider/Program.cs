using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DbProviderFactory>();

var app = builder.Build();

app.Urls.Add("http://127.0.0.1:5130");

app.MapPost("/providers", (DbProviderFactory factory) =>
{
    try
    {
        var result = factory.GetDbProvider("mssql");
        Console.WriteLine($"Provider retrieved: {result.GetType().Name}");
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Operation failed with error: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

public interface IDbProvider { }

public class MssqlProvider : IDbProvider { }

public class PostgreSqlProvider : IDbProvider { }

public class DbProviderFactory
{
    private readonly ConcurrentDictionary<string, IDbProvider> _providers = new();

    public IDbProvider GetDbProvider(string key)
    {
        Console.WriteLine($"Attempting to get provider for key: {key}");

        return _providers.GetOrAdd(key, providerKey =>
        {
            Console.WriteLine($"Provider not found for key {providerKey}, creating new instance");

            IDbProvider provider = providerKey switch
            {
                "mssql" => new MssqlProvider(),
                "postgres" => new PostgreSqlProvider(),
                _ => throw new NotSupportedException($"Database type {providerKey} is not supported")
            };

            Console.WriteLine($"Created new provider for key: {providerKey}");

            return provider;
        });
        
        
    }
}