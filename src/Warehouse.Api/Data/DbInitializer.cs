using Dapper;

namespace Warehouse.Api.Data;

/// <summary>
/// Migrador mínimo: ejecuta en orden los scripts .sql de la carpeta Migrations que
/// aún no se hayan aplicado, registrándolos en la tabla schema_migrations.
/// Sin dependencias externas y control total del SQL (estilo del proyecto).
/// </summary>
public static class DbInitializer
{
    public static async Task RunAsync(IServiceProvider services, IWebHostEnvironment env, ILogger logger)
    {
        var factory = services.GetRequiredService<DbConnectionFactory>();
        using var conn = await factory.OpenAsync();

        await conn.ExecuteAsync(
            "create table if not exists schema_migrations (id text primary key, applied_at timestamptz not null default now());");

        var applied = (await conn.QueryAsync<string>("select id from schema_migrations"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dir = Path.Combine(env.ContentRootPath, "Migrations");
        if (!Directory.Exists(dir))
        {
            logger.LogWarning("No existe la carpeta de migraciones: {Dir}", dir);
            return;
        }

        var files = Directory.GetFiles(dir, "*.sql").OrderBy(Path.GetFileName, StringComparer.Ordinal);
        foreach (var file in files)
        {
            var id = Path.GetFileName(file);
            if (applied.Contains(id)) continue;

            var sql = await File.ReadAllTextAsync(file);
            using var tx = conn.BeginTransaction();
            await conn.ExecuteAsync(sql, transaction: tx);
            await conn.ExecuteAsync(
                "insert into schema_migrations (id) values (@id)", new { id }, tx);
            tx.Commit();
            logger.LogInformation("Migración aplicada: {Id}", id);
        }
    }
}
