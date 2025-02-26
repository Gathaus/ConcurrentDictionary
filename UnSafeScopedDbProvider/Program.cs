var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<DbProviderFactory>(); 

var app = builder.Build();

app.Urls.Add("http://127.0.0.1:5130");

app.MapPost("/providers", async (DbProviderFactory factory) =>
{
    var tasks = new List<Task<IDbProvider>>();

    // 5 tane paralel task çalıştırılıyor
    for (int i = 0; i < 5; i++)
    {
        tasks.Add(Task.Run(() => factory.GetDbProvider("mssql")));
    }

    try
    {
        var results = await Task.WhenAll(tasks);
        return Results.Ok(results);
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
    private readonly Dictionary<string, IDbProvider> _providers = new();

    public IDbProvider GetDbProvider(string key)
    {
        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Attempting to get provider for key: {key}");

        if (!_providers.ContainsKey(key))
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Provider not found for key {key}, creating new instance");

            Thread.Sleep(100); // Thread'leri yarışa sokmak için gecikme ekliyoruz
            IDbProvider provider = key switch
            {
                "mssql" => new MssqlProvider(),
                "postgres" => new PostgreSqlProvider(),
                _ => throw new NotSupportedException($"Database type {key} is not supported")
            };

            _providers[key] = provider; 

            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Created new provider for key: {key}");
        }

        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Retrieved existing provider for key: {key}");
        return _providers[key];
    }
}
