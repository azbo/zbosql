using System.Linq.Expressions;
using ZboSql.Core.Infrastructure;
using ZboSql.Core.Expressions;
using ZboSql.PostgreSql;

namespace ZboSql.PostgreSql;

/// <summary>
/// 更新接口
/// </summary>
public sealed class Updateable<TSource> where TSource : class
{
    private readonly NpgsqlProvider _provider;
    private readonly PostgresSqlBuilder _builder;
    private readonly SourceInfo _sourceInfo;
    private readonly TSource _entity;
    private Expression<Func<TSource, bool>>? _whereExpression;

    internal Updateable(NpgsqlProvider provider, SourceInfo sourceInfo, TSource entity)
    {
        _provider = provider;
        _builder = new PostgresSqlBuilder(provider);
        _sourceInfo = sourceInfo;
        _entity = entity;
    }

    /// <summary>
    /// WHERE 条件
    /// </summary>
    public Updateable<TSource> Where(Expression<Func<TSource, bool>> predicate)
    {
        _whereExpression = predicate;
        return this;
    }

    /// <summary>
    /// 执行更新
    /// </summary>
    public int Execute()
    {
        var sql = _builder.BuildUpdate(_whereExpression, _sourceInfo);
        var entityInfo = EntityInfo.Get<TSource>();
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(_whereExpression);

        var command = _provider.CreateCommand(sql, whereResult.Parameters.ToArray());

        // 添加 SET 参数（非主键列）
        foreach (var column in entityInfo.Columns.Where(c => !c.IsPrimaryKey))
        {
            var value = column.PropertyInfo.GetValue(_entity);
            var parameter = command.CreateParameter();
            parameter.ParameterName = _provider.FormatParameterName(column.PropertyName);
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// 异步执行更新
    /// </summary>
    public async Task<int> ExecuteAsync()
    {
        var sql = _builder.BuildUpdate(_whereExpression, _sourceInfo);
        var entityInfo = EntityInfo.Get<TSource>();
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(_whereExpression);

        var command = _provider.CreateCommand(sql, whereResult.Parameters.ToArray());

        // 添加 SET 参数（非主键列）
        foreach (var column in entityInfo.Columns.Where(c => !c.IsPrimaryKey))
        {
            var value = column.PropertyInfo.GetValue(_entity);
            var parameter = command.CreateParameter();
            parameter.ParameterName = _provider.FormatParameterName(column.PropertyName);
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return await command.ExecuteNonQueryAsync();
    }
}
