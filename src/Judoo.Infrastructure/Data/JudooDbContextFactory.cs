using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Judoo.Infrastructure.Data;

public class JudooDbContextFactory : IDesignTimeDbContextFactory<JudooDbContext>
{
    public JudooDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JudooDbContext>();
        
        // String de conexão padrão de desenvolvimento / migrations
        const string connectionString = "Host=localhost;Port=5433;Database=judoo_db_dev;Username=judoo_user;Password=judoo_secret;";
        
        optionsBuilder.UseNpgsql(connectionString, b =>
        {
            b.MigrationsAssembly(typeof(JudooDbContext).Assembly.FullName);
        });

        return new JudooDbContext(optionsBuilder.Options);
    }
}
