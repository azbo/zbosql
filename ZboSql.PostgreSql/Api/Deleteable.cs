using System.Linq.Expressions;
using ZboSql.Core.Infrastructure;
using ZboSql.Core.Expressions;
using ZboSql.PostgreSql;

namespace ZboSql.PostgreSql;

/// <summary>
/// 删除接口
/// </summary>
public sealed class Deleteable<TSource> where TSource : class
{
    private readonly NpgsqlProvider _provider;
    private readonly PostgresSqlBuilder _builder;
    private readonly SourceInfo _sourceInfo;
    private Expression<Func<TSource, bool>>? _whereExpression;

    internal Deleteable(NpgsqlProvider provider, SourceInfo sourceInfo)
    {
        _provider = provider;
        _builder = new PostgresSqlBuilder(provider);
        _sourceInfo = sourceInfo;
    }

    /// <summary>
    /// WHERE 条件
    /// </summary>
    public Deleteable<TSource> Where(Expression<Func<TSource, bool>> predicate)
    {
        _whereExpression = predicate;
        return this;
    }

    /// <summary>
    /// 执行删除
    /// </summary>
    public int Execute()
    {
        var sql = _builder.BuildDelete(_whereExpression, _sourceInfo);
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(_whereExpression);

        var command = _provider.CreateCommand(sql, whereResult.Parameters.ToArray());
        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// 异步执行删除
    /// </summary>
    public async Task<int> ExecuteAsync()
    {
        var sql = _builder.BuildDelete(_whereExpression, _sourceInfo);
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(_whereExpression);

        var command = _provider.CreateCommand(sql, whereResult.Parameters.ToArray());
        return await command.ExecuteNonQueryAsync();
    }
}
