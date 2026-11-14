using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Judoo.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Port=5433;Database=judoo_db_dev;Username=judoo_user;Password=judoo_secret;";
    }

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        return connection;
    }
}
