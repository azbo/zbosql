using System.Data;
using System.Linq.Expressions;
using ZboSql.Core.Infrastructure;
using ZboSql.Core.Expressions;

namespace ZboSql.PostgreSql;

/// <summary>
/// 表达式参数替换器（用于合并 Where 条件）
/// </summary>
internal class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _parameter;

    public ParameterReplacer(ParameterExpression parameter)
    {
        _parameter = parameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        // 统一替换为新的参数
        return _parameter;
    }
}

/// <summary>
/// 查询接口
/// </summary>
public sealed class Queryable<TSource>
{
    private readonly NpgsqlProvider _provider;
    private readonly PostgresSqlBuilder _builder;
    private readonly SourceInfo _sourceInfo;
    private readonly Type? _sourceEntityType;  // 原始实体类型（Select 后使用）

    private LambdaExpression? _whereExpression;
    private LambdaExpression? _orderByExpression;
    private bool _orderByDescending;
    private int? _skip;
    private int? _take;
    private SelectParseResult? _selectResult;

    internal Queryable(NpgsqlProvider provider, SourceInfo sourceInfo)
    {
        _provider = provider;
        _builder = new PostgresSqlBuilder(provider);
        _sourceInfo = sourceInfo;
        _sourceEntityType = typeof(TSource);  // 默认就是 TSource
    }

    // 用于 Select 后创建新的 Queryable
    internal Queryable(NpgsqlProvider provider, SourceInfo sourceInfo, SelectParseResult selectResult,
        Type sourceEntityType,  // 原始实体类型
        LambdaExpression? whereExpression, LambdaExpression? orderByExpression,
        bool orderByDescending, int? skip, int? take)
        : this(provider, sourceInfo)
    {
        _selectResult = selectResult;
        _sourceEntityType = sourceEntityType;  // 保存原始实体类型
        _whereExpression = whereExpression;
        _orderByExpression = orderByExpression;
        _orderByDescending = orderByDescending;
        _skip = skip;
        _take = take;
    }

    /// <summary>
    /// WHERE 条件
    /// </summary>
    public Queryable<TSource> Where(Expression<Func<TSource, bool>> predicate)
    {
        if (_whereExpression == null)
        {
            _whereExpression = predicate;
        }
        else
        {
            // 合并多个 Where 条件：使用 AndAlso 连接
            var parameter = Expression.Parameter(typeof(TSource), "x");

            // 替换第一个表达式的参数
            var firstBody = new ParameterReplacer(parameter).Visit(_whereExpression.Body);
            // 替换第二个表达式的参数
            var secondBody = new ParameterReplacer(parameter).Visit(predicate.Body);

            // 组合：first && second
            var combinedBody = Expression.AndAlso(firstBody, secondBody);
            _whereExpression = Expression.Lambda<Func<TSource, bool>>(combinedBody, parameter);
        }

        return this;
    }

    /// <summary>
    /// ORDER BY 升序
    /// </summary>
    public Queryable<TSource> OrderBy<TKey>(Expression<Func<TSource, TKey>> keySelector)
    {
        _orderByExpression = keySelector as LambdaExpression;
        _orderByDescending = false;
        return this;
    }

    /// <summary>
    /// ORDER BY 降序
    /// </summary>
    public Queryable<TSource> OrderByDescending<TKey>(Expression<Func<TSource, TKey>> keySelector)
    {
        _orderByExpression = keySelector as LambdaExpression;
        _orderByDescending = true;
        return this;
    }

    /// <summary>
    /// 跳过前 N 条
    /// </summary>
    public Queryable<TSource> Skip(int count)
    {
        _skip = count;
        return this;
    }

    /// <summary>
    /// 取前 N 条
    /// </summary>
    public Queryable<TSource> Take(int count)
    {
        _take = count;
        return this;
    }

    /// <summary>
    /// 投影到新类型（自动映射同名属性）
    /// </summary>
    public Queryable<TResult> Select<TResult>() where TResult : class, new()
    {
        // 分析 Where 条件
        var whereAnalysis = SelectProjectionOptimizer.AnalyzeWhere(_whereExpression);

        var parser = new SelectExpressionParser(_provider);
        parser.SetWhereAnalysis(whereAnalysis);
        var selectResult = parser.ParseAuto<TSource, TResult>();

        // 创建一个新的 Queryable<TResult>
        return SelectInternal<TResult>(selectResult);
    }

    /// <summary>
    /// 投影到新类型（支持匿名对象、DTO 类）
    /// </summary>
    public Queryable<TResult> Select<TResult>(Expression<Func<TSource, TResult>> selector)
    {
        // 分析 Where 条件
        var whereAnalysis = SelectProjectionOptimizer.AnalyzeWhere(_whereExpression);

        var parser = new SelectExpressionParser(_provider);
        parser.SetWhereAnalysis(whereAnalysis);
        var selectResult = parser.Parse(selector);

        // 创建一个新的 Queryable<TResult>
        return SelectInternal<TResult>(selectResult);
    }

    // 内部方法：创建 Select 后的 Queryable
    private Queryable<TResult> SelectInternal<TResult>(SelectParseResult selectResult)
    {
        return new Queryable<TResult>(_provider, _sourceInfo, selectResult,
            typeof(TSource),  // 原始实体类型
            _whereExpression, _orderByExpression, _orderByDescending, _skip, _take);
    }

    /// <summary>
    /// 执行查询并返回列表
    /// </summary>
    public List<TSource> ToList()
    {
        var selectClause = _selectResult?.SelectClause ?? null!;

        // 使用原始实体类型生成 SQL
        var entityType = _sourceEntityType ?? typeof(TSource);
        var buildSelectMethod = typeof(PostgresSqlBuilder).GetMethod(nameof(PostgresSqlBuilder.BuildSelect), new Type[]
        {
            typeof(LambdaExpression), typeof(LambdaExpression), typeof(bool), typeof(int?), typeof(int?),
            typeof(SourceInfo), typeof(string), typeof(Type)
        });
        var sql = (string)buildSelectMethod.Invoke(_builder, new object[]
        {
            _whereExpression, _orderByExpression, _orderByDescending, _skip, _take, _sourceInfo, selectClause, entityType
        });

        // 使用原始实体类型解析 Where 表达式
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(entityType, _whereExpression);

        var command = _provider.CreateCommand(sql, whereResult.Parameters.ToArray());
        using var reader = command.ExecuteReader();

        // 如果有 Select，使用映射器
        if (_selectResult != null)
        {
            var results = new List<TSource>();

            // 检查是否为简单类型
            var isSimpleType = typeof(TSource).IsValueType || typeof(TSource) == typeof(string);

            if (_selectResult.Projections.Count == 1 && isSimpleType)
            {
                // 单字段简单类型选择
                while (reader.Read())
                {
                    var value = SelectMapper.MapSingleValue<TSource>(reader, _selectResult.Projections[0].SourceColumn);
                    results.Add(value);
                }
            }
            else if (_selectResult.AllFieldsAreFixed)
            {
                // 所有字段都是固定值，不需要从 reader 读取数据
                while (reader.Read())
                {
                    var item = SelectMapper.MapToTypeWithoutReader<TSource>(_selectResult);
                    results.Add(item);
                }
            }
            else
            {
                // 复杂类型选择
                while (reader.Read())
                {
                    var item = SelectMapper.MapToType<TSource>(reader, _selectResult);
                    results.Add(item);
                }
            }

            return results;
        }

        // 否则返回实体列表
        var entities = new List<TSource>();
        while (reader.Read())
        {
            var entity = MapEntity(reader);
            entities.Add(entity);
        }

        return entities;
    }

    /// <summary>
    /// 异步执行查询并返回列表
    /// </summary>
    public async Task<List<TSource>> ToListAsync()
    {
        var selectClause = _selectResult?.SelectClause ?? null!;

        // 使用原始实体类型生成 SQL
        var entityType = _sourceEntityType ?? typeof(TSource);
        var buildSelectMethod = typeof(PostgresSqlBuilder).GetMethod(nameof(PostgresSqlBuilder.BuildSelect), new Type[]
        {
            typeof(LambdaExpression), typeof(LambdaExpression), typeof(bool), typeof(int?), typeof(int?),
            typeof(SourceInfo), typeof(string), typeof(Type)
        });
        var sql = (string)buildSelectMethod.Invoke(_builder, new object[]
        {
            _whereExpression, _orderByExpression, _orderByDescending, _skip, _take, _sourceInfo, selectClause, entityType
        });

        // 使用原始实体类型解析 Where 表达式
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(entityType, _whereExpression);

        var command = _provider.CreateCommand(sql, whereResult.Parameters.ToArray());
        using var reader = await command.ExecuteReaderAsync();

        // 如果有 Select，使用映射器
        if (_selectResult != null)
        {
            var results = new List<TSource>();

            // 检查是否为简单类型
            var isSimpleType = typeof(TSource).IsValueType || typeof(TSource) == typeof(string);

            if (_selectResult.Projections.Count == 1 && isSimpleType)
            {
                // 单字段简单类型选择
                while (await reader.ReadAsync())
                {
                    var value = SelectMapper.MapSingleValue<TSource>(reader, _selectResult.Projections[0].SourceColumn);
                    results.Add(value);
                }
            }
            else if (_selectResult.AllFieldsAreFixed)
            {
                // 所有字段都是固定值，不需要从 reader 读取数据
                while (await reader.ReadAsync())
                {
                    var item = SelectMapper.MapToTypeWithoutReader<TSource>(_selectResult);
                    results.Add(item);
                }
            }
            else
            {
                // 复杂类型选择
                while (await reader.ReadAsync())
                {
                    var item = SelectMapper.MapToType<TSource>(reader, _selectResult);
                    results.Add(item);
                }
            }

            return results;
        }

        // 否则返回实体列表
        var entities = new List<TSource>();
        while (await reader.ReadAsync())
        {
            var entity = MapEntity(reader);
            entities.Add(entity);
        }

        return entities;
    }

    /// <summary>
    /// 返回第一条记录
    /// </summary>
    public TSource? FirstOrDefault()
    {
        _take = 1;
        var list = ToList();
        return list.FirstOrDefault();
    }

    /// <summary>
    /// 异步返回第一条记录
    /// </summary>
    public async Task<TSource?> FirstOrDefaultAsync()
    {
        _take = 1;
        var list = await ToListAsync();
        return list.FirstOrDefault();
    }

    /// <summary>
    /// 返回记录数量
    /// </summary>
    public int Count()
    {
        // 使用原始实体类型生成 SQL
        var entityType = _sourceEntityType ?? typeof(TSource);
        var buildCountMethod = typeof(PostgresSqlBuilder).GetMethod(nameof(PostgresSqlBuilder.BuildCount), new Type[]
        {
            typeof(LambdaExpression), typeof(SourceInfo), typeof(Type)
        });
        var sql = (string)buildCountMethod.Invoke(_builder, new object[] { _whereExpression, _sourceInfo, entityType });

        // 使用原始实体类型解析 Where 表达式
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(entityType, _whereExpression);

        var command = _provider.CreateCommand(sql, whereResult.Parameters.ToArray());
        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    /// <summary>
    /// 异步返回记录数量
    /// </summary>
    public async Task<int> CountAsync()
    {
        // 使用原始实体类型生成 SQL
        var entityType = _sourceEntityType ?? typeof(TSource);
        var buildCountMethod = typeof(PostgresSqlBuilder).GetMethod(nameof(PostgresSqlBuilder.BuildCount), new Type[]
        {
            typeof(LambdaExpression), typeof(SourceInfo), typeof(Type)
        });
        var sql = (string)buildCountMethod.Invoke(_builder, new object[] { _whereExpression, _sourceInfo, entityType });

        // 使用原始实体类型解析 Where 表达式
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(entityType, _whereExpression);

        var command = _provider.CreateCommand(sql, whereResult.Parameters.ToArray());
        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    /// <summary>
    /// 映射实体（暂时使用反射，表达式树优化待完善）
    /// </summary>
    private TSource MapEntity(IDataReader reader)
    {
        var entityInfo = EntityInfo.Get<TSource>();
        var entity = Activator.CreateInstance<TSource>();

        foreach (var column in entityInfo.Columns)
        {
            var ordinal = reader.GetOrdinal(column.ColumnName);
            if (!reader.IsDBNull(ordinal))
            {
                var value = reader.GetValue(ordinal);
                var convertedValue = Convert.ChangeType(value, column.PropertyType);
                column.PropertyInfo.SetValue(entity, convertedValue);
            }
        }

        return entity;
    }
}
