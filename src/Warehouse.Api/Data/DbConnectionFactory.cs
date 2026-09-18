using System.Data;
using Npgsql;

namespace Warehouse.Api.Data;

/// <summary>Crea conexiones PostgreSQL abiertas a partir de la cadena de conexión configurada.</summary>
public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Postgres'.");
    }

    public string ConnectionString => _connectionString;

    public async Task<IDbConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
