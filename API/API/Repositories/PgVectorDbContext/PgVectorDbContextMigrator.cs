using Microsoft.EntityFrameworkCore;

namespace API.Repositories.PgVectorDbContext;

public class PgVectorDbContextMigrator : IHostedService
{
    private readonly IServiceProvider _sp;

    // You can use either IDbContextFactory or IServiceProvider scope with DbContext
    public PgVectorDbContextMigrator(IDbContextFactory<PgVectorDbContext>? factory, IServiceProvider sp)
    {
        _sp = sp;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<PgVectorDbContext>();
        await ctx.Database.MigrateAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}