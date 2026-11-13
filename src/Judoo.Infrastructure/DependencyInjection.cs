using Judoo.Application.Common.Interfaces;
using Judoo.Domain.Repositories;
using Judoo.Domain.Services;
using Judoo.Infrastructure.Data;
using Judoo.Infrastructure.Repositories;
using Judoo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Judoo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=judoo_db;Username=judoo_user;Password=judoo_secret;";

        // 1. Entity Framework Core (para Modelagem, Mapeamentos e Migrations)
        services.AddDbContext<JudooDbContext>(options =>
        {
            options.UseNpgsql(connectionString, b =>
            {
                b.MigrationsAssembly(typeof(JudooDbContext).Assembly.FullName);
            });
        });

        // 2. Dapper Factory (para Leituras e Escritas de Ultra-Alta Performance)
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        // 3. Repositórios Dapper
        services.AddScoped<ILutaRepository, LutaRepository>();
        services.AddScoped<IPlacarLutaRepository, PlacarLutaRepository>();
        services.AddScoped<ITatameRepository, TatameRepository>();
        services.AddScoped<ICampeonatoRepository, CampeonatoRepository>();
        services.AddScoped<IAtletaRepository, AtletaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        // 4. Segurança e SignalR
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSignalR();
        services.AddScoped<ITatameNotificationService, TatameNotificationService>();

        return services;
    }
}
