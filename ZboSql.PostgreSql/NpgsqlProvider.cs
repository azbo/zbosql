using Npgsql;
using ZboSql.Core.Infrastructure;

namespace ZboSql.PostgreSql;

/// <summary>
/// PostgreSQL 数据库提供程序
/// </summary>
public sealed class NpgsqlProvider : DbProviderBase
{
    public override string DbName => "PostgreSQL";
    public override DbType DbType => DbType.PostgreSql;
    protected override string QuoteChar => "\"";
    protected override string ParameterPrefix => "@";

    protected override System.Data.Common.DbConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }

    public override string BuildLimitClause(int? skip, int? take)
    {
        var clauses = new List<string>();
        if (take.HasValue)
            clauses.Add($"LIMIT {take.Value}");
        if (skip.HasValue)
            clauses.Add($"OFFSET {skip.Value}");
        return string.Join(" ", clauses);
    }
}
