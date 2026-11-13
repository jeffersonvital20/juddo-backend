using Judoo.Domain.Repositories;
using Judoo.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Judoo.IntegrationTests.Fixtures;

public class JudooApiFactory : WebApplicationFactory<Program>
{
    private readonly string _inMemoryDbName = $"JudooIntegrationDb_{Guid.NewGuid()}";
    public InMemoryTestStore Store { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<JudooDbContext>>();
            services.RemoveAll<JudooDbContext>();
            services.AddDbContext<JudooDbContext>(options =>
                options.UseInMemoryDatabase(_inMemoryDbName));

            services.RemoveAll<ICampeonatoRepository>();
            services.RemoveAll<ITatameRepository>();
            services.RemoveAll<IAtletaRepository>();
            services.RemoveAll<ILutaRepository>();
            services.RemoveAll<IPlacarLutaRepository>();
            services.RemoveAll<IUsuarioRepository>();

            services.AddSingleton(Store);
            services.AddScoped<ICampeonatoRepository, InMemoryCampeonatoRepository>();
            services.AddScoped<ITatameRepository, InMemoryTatameRepository>();
            services.AddScoped<IAtletaRepository, InMemoryAtletaRepository>();
            services.AddScoped<ILutaRepository, InMemoryLutaRepository>();
            services.AddScoped<IPlacarLutaRepository, InMemoryPlacarLutaRepository>();
            services.AddScoped<IUsuarioRepository, InMemoryUsuarioRepository>();
        });
    }
}
