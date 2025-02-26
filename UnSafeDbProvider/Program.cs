var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DbProviderFactory>();

var app = builder.Build();

app.Urls.Add("http://127.0.0.1:5130");

app.MapPost("/providers", (DbProviderFactory factory) =>
{
    try
    {
        var result = factory.GetDbProvider("mssql");

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Operation failed with error: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

public interface IDbProvider;

public class MssqlProvider : IDbProvider;

public class PostgreSqlProvider : IDbProvider;

public class DbProviderFactory
{
    private readonly Dictionary<string, IDbProvider> _providers = new();

    public IDbProvider GetDbProvider(string key)
    {
        Console.WriteLine($"Attempting to get provider for key: {key}");

        if (!_providers.ContainsKey(key))
        {
            Console.WriteLine($"Provider not found for key {key}, creating new instance");

            Thread.Sleep(100);
            IDbProvider provider = key switch
            {
                "mssql" => new MssqlProvider(),
                "postgres" => new PostgreSqlProvider(),
                _ => throw new NotSupportedException($"Database type {key} is not supported")
            };

            _providers[key] = provider;

            Console.WriteLine($"Created new provider for key: {key}");
        }

        Console.WriteLine($"Retrieved existing provider for key: {key}");
        return _providers[key];
    }
}