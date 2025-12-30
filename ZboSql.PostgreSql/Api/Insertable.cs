using System.Data;
using ZboSql.Core.Infrastructure;
using ZboSql.PostgreSql;

namespace ZboSql.PostgreSql;

/// <summary>
/// 插入接口
/// </summary>
public sealed class Insertable<TSource> where TSource : class
{
    private readonly NpgsqlProvider _provider;
    private readonly PostgresSqlBuilder _builder;
    private readonly SourceInfo _sourceInfo;
    private readonly TSource _entity;

    internal Insertable(NpgsqlProvider provider, SourceInfo sourceInfo, TSource entity)
    {
        _provider = provider;
        _builder = new PostgresSqlBuilder(provider);
        _sourceInfo = sourceInfo;
        _entity = entity;
    }

    /// <summary>
    /// 执行插入
    /// </summary>
    public int Execute()
    {
        var sql = _builder.BuildInsert<TSource>(_sourceInfo);
        var entityInfo = EntityInfo.Get<TSource>();

        var command = _provider.CreateCommand(sql);

        // 添加参数
        foreach (var column in entityInfo.Columns.Where(c => !c.IsIdentity))
        {
            var value = column.PropertyInfo.GetValue(_entity);
            var parameter = command.CreateParameter();
            parameter.ParameterName = _provider.FormatParameterName(column.PropertyName);
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        // 如果有返回自增主键，则执行标量查询
        if (entityInfo.PrimaryKey?.IsIdentity == true)
        {
            var result = command.ExecuteScalar();
            if (result != null && entityInfo.PrimaryKey != null)
            {
                var id = Convert.ToInt32(result);
                entityInfo.PrimaryKey.PropertyInfo.SetValue(_entity, id);
            }
            return 1;
        }

        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// 异步执行插入
    /// </summary>
    public async Task<int> ExecuteAsync()
    {
        var sql = _builder.BuildInsert<TSource>(_sourceInfo);
        var entityInfo = EntityInfo.Get<TSource>();

        var command = _provider.CreateCommand(sql);

        // 添加参数
        foreach (var column in entityInfo.Columns.Where(c => !c.IsIdentity))
        {
            var value = column.PropertyInfo.GetValue(_entity);
            var parameter = command.CreateParameter();
            parameter.ParameterName = _provider.FormatParameterName(column.PropertyName);
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        // 如果有返回自增主键，则执行标量查询
        if (entityInfo.PrimaryKey?.IsIdentity == true)
        {
            var result = await command.ExecuteScalarAsync();
            if (result != null && entityInfo.PrimaryKey != null)
            {
                var id = Convert.ToInt32(result);
                entityInfo.PrimaryKey.PropertyInfo.SetValue(_entity, id);
            }
            return 1;
        }

        return await command.ExecuteNonQueryAsync();
    }
}
