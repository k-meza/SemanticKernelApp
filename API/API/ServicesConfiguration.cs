using API.Domain.SemanticKernel;
using API.Domain.SemanticKernel.Interfaces;
using API.Domain.SessionManager;
using API.Options;
using API.Repositories.PgVectorDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace API;

public static class ServicesConfiguration
{
    public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.AddOptionsAsSelf<OpenAiSettings>(configuration.GetSection("OpenAiSettings"));

        // Hubs
        services.AddSignalR();

        // Domain
        services.AddSingleton<ISessionManager, InMemorySessionManager>();
        services.AddSingleton<ISemanticKernelFactory, SemanticKernelFactory>();


        // Repositories
        // 1) Get connection string
        var cs = configuration.GetConnectionString("PgVectorDbContext");

        // 2) Build an NpgsqlDataSource with pgvector enabled
        var dsb = new NpgsqlDataSourceBuilder(cs);
        dsb.UseVector(); // <-- IMPORTANT: enables pgvector type mapping
        var dataSource = dsb.Build();

        // 3) Register in DI
        services.AddSingleton(dataSource);

        services.AddDbContext<PgVectorDbContext>((sp, opts) =>
        {
            var ds = sp.GetRequiredService<NpgsqlDataSource>();
            opts.UseNpgsql(ds, npg =>
            {
                npg.EnableRetryOnFailure();
            });
        });
        
        // Run migrations on startup in Dev
        services.AddHostedService(sp => new PgVectorDbContextMigrator(sp.GetRequiredService<IDbContextFactory<PgVectorDbContext>>(),
            sp.GetRequiredService<IServiceProvider>()));


        // Clients
    }

    /// <summary>
    /// Register an <c>IOptions</c> configuration instance and also a standalone instance of the <typeparamref name="TOptions"/> class.<para/>
    /// For example, <c>ConfigureWithSelf&lt;FooSettings&gt;(...)</c> will register both <c>IOptions&lt;FooSettings&gt;</c> and <c>FooSettings</c>.
    /// </summary>
    /// <typeparam name="TOptions">Type of options to configure</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration section. Use <c>configuration.GetSection("FooSettings")</c> to get a specific named element.</param>
    static IServiceCollection AddOptionsAsSelf<TOptions>(this IServiceCollection services, IConfiguration configuration)
        where TOptions : class, new()
    {
        services.AddOptions<TOptions>().Bind(configuration);
        return services.AddTransient(sp => sp.GetService<IOptions<TOptions>>().Value);
    }

    /// <summary>
    /// Register a scoped service as both a <typeparamref name="TInterface"/> and a <see cref="Lazy{T}" />, bound to <typeparamref name="TType"/>.
    /// </summary>
    /// <typeparam name="TInterface">Service interface</typeparam>
    /// <typeparam name="TType">Service type</typeparam>
    /// <param name="services">Service collection</param>
    public static IServiceCollection AddScopedLazy<TInterface, TType>(this IServiceCollection services)
        where TType : class, TInterface
        where TInterface : class
    {
        return services.AddScoped<TInterface, TType>()
            .AddScoped(sp => new Lazy<TInterface>(sp.GetRequiredService<TInterface>));
    }
}