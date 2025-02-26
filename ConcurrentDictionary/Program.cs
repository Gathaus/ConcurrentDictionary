using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ThreadSafeDbProviderFactory>();
builder.Services.AddSingleton<UnsafeDbProviderFactory>();

var app = builder.Build();

app.Urls.Add("http://127.0.0.1:5130");

app.MapPost("/providers/safe", (ThreadSafeDbProviderFactory factory, ILogger<Program> logger) =>
{
    var startTime = DateTime.UtcNow;
    var result = factory.GetDbProvider("mssql-tr-ist");
    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
    
    logger.LogInformation("[Safe] Provider retrieved in {Duration}ms. Provider Count: {Count}",
        duration, factory.ProviderCount);
    
    return Results.Ok(new { providerId = result.ProviderId, providerType = result.ProviderType, count = factory.ProviderCount, duration });
});

app.MapPost("/providers/unsafe", (UnsafeDbProviderFactory factory, ILogger<Program> logger) =>
{
    try
    {
        var startTime = DateTime.UtcNow;
        var result = factory.GetDbProvider("mssql-tr-ist");
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        
        logger.LogInformation("[Unsafe] Provider retrieved in {Duration}ms. Provider Count: {Count}",
            duration, factory.ProviderCount);
        
        return Results.Ok(new { providerId = result.ProviderId, providerType = result.ProviderType, count = factory.ProviderCount, duration });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Unsafe] Operation failed with error: {Error}", ex.Message);
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

public interface IDbProvider
{
    string ProviderId { get; }
    string Region { get; }
    string ProviderType { get; }
    bool IsActive { get; }
    string ConnectionString { get; }
}

public class DbProvider : IDbProvider
{
    public string ProviderId { get; set; }
    public string Region { get; set; }
    public string ProviderType { get; set; }
    public bool IsActive { get; set; }
    public string ConnectionString { get; set; }
}

public class MssqlProvider : DbProvider
{
    public MssqlProvider(string region)
    {
        ProviderId = $"{region}-{Guid.NewGuid():N}";
        Region = region;
        ProviderType = "MSSQL";
        IsActive = true;
        ConnectionString = $"Server=mssql-{region.ToLower()};Database=mydb;User Id=admin;Password=****;";
    }
}

public class PostgresProvider : DbProvider
{
    public PostgresProvider(string region)
    {
        ProviderId = $"{region}-{Guid.NewGuid():N}";
        Region = region;
        ProviderType = "PostgreSQL";
        IsActive = true;
        ConnectionString = $"Host=postgres-{region.ToLower()};Database=mydb;Username=admin;Password=****;";
    }
}

// Thread-safe implementation using ConcurrentDictionary
public class ThreadSafeDbProviderFactory
{
    private const string MssqlType = "mssql";
    private const string PostgresType = "postgres";
    private readonly ConcurrentDictionary<string, IDbProvider> _providers = new();
    private readonly ILogger<ThreadSafeDbProviderFactory> _logger;

    public int ProviderCount => _providers.Count;

    public ThreadSafeDbProviderFactory(ILogger<ThreadSafeDbProviderFactory> logger)
    {
        _logger = logger;
    }

    public IDbProvider GetDbProvider(string key)
    {
        _logger.LogDebug("[Safe] Attempting to get provider for key: {Key}", key);
        
        return _providers.GetOrAdd(key, providerKey =>
        {
            _logger.LogDebug("[Safe] Provider not found for key {Key}, creating new instance", providerKey);
            
            // Simulate some initialization work
            Thread.Sleep(100);
            
            var parts = providerKey.Split('-');
            var providerType = parts[0].ToLower();
            var region = string.Join("-", parts.Skip(1));
            
            IDbProvider provider = providerType == MssqlType 
                ? new MssqlProvider(region)
                : providerType == PostgresType 
                    ? new PostgresProvider(region)
                    : throw new NotSupportedException($"Database provider type {providerType} is not supported");
            
            _logger.LogDebug("[Safe] Created new provider with ID: {ProviderId} of type: {ProviderType} for key: {Key}",
                provider.ProviderId, provider.ProviderType, providerKey);
                
            return provider;
        });
    }
}

// Unsafe implementation using Dictionary
public class UnsafeDbProviderFactory
{
    private const string MssqlType = "mssql";
    private const string PostgresType = "postgres";
    private readonly Dictionary<string, IDbProvider> _providers = new();
    private readonly ILogger<UnsafeDbProviderFactory> _logger;

    public int ProviderCount => _providers.Count;

    public UnsafeDbProviderFactory(ILogger<UnsafeDbProviderFactory> logger)
    {
        _logger = logger;
    }

    public IDbProvider GetDbProvider(string key)
    {
        _logger.LogDebug("[Unsafe] Checking provider for key: {Key}", key);
        
        if (!_providers.ContainsKey(key))
        {
            _logger.LogDebug("[Unsafe] Provider not found for key {Key}, creating new instance", key);
            
            // Simulate some initialization work
            Thread.Sleep(100);
            
            var parts = key.Split('-');
            var providerType = parts[0].ToLower();
            var region = string.Join("-", parts.Skip(1));
            
            IDbProvider provider = providerType == MssqlType 
                ? new MssqlProvider(region)
                : providerType == PostgresType 
                    ? new PostgresProvider(region)
                    : throw new NotSupportedException($"Database provider type {providerType} is not supported");
            
            _providers[key] = provider; // Potential race condition here!
            
            _logger.LogDebug("[Unsafe] Created new provider with ID: {ProviderId} of type: {ProviderType} for key: {Key}",
                provider.ProviderId, provider.ProviderType, key);
                
            return provider;
        }
        
        _logger.LogDebug("[Unsafe] Retrieved existing provider for key: {Key}", key);
        return _providers[key];
    }
}