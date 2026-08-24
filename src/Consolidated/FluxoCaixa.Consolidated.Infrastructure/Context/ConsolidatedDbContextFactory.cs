using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FluxoCaixa.Consolidated.Infrastructure.Context;

public class ConsolidatedDbContextFactory : IDesignTimeDbContextFactory<ConsolidatedDbContext>
{
    public ConsolidatedDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ConsolidatedDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=fluxocaixa_consolidated;Username=postgres;Password=postgres");

        return new ConsolidatedDbContext(optionsBuilder.Options);
    }
}
