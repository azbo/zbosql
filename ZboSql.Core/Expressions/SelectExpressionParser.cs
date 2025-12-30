using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using ZboSql.Core.Infrastructure;

namespace ZboSql.Core.Expressions;

/// <summary>
/// Select 投影信息
/// </summary>
public sealed class SelectProjection
{
    public string? SourceColumn { get; set; }  // null 表示使用固定值
    public string TargetProperty { get; set; } = string.Empty;
    public Type TargetType { get; set; } = typeof(object);
    public object? FixedValue { get; set; }  // Where 中的固定值
}

/// <summary>
/// Select 表达式解析结果
/// </summary>
public sealed class SelectParseResult
{
    public List<SelectProjection> Projections { get; set; } = new();
    public Type ResultType { get; set; } = typeof(object);
    public string SelectClause { get; set; } = "*";
    /// <summary>
    /// 是否所有投影字段都是固定值（不需要从数据库查询）
    /// </summary>
    public bool AllFieldsAreFixed { get; set; }
}

/// <summary>
/// Select 表达式解析器
/// </summary>
public sealed class SelectExpressionParser
{
    private readonly IDbProvider _provider;
    private WhereAnalysis? _whereAnalysis;

    public SelectExpressionParser(IDbProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// 设置 Where 分析结果（用于投影优化）
    /// </summary>
    public void SetWhereAnalysis(WhereAnalysis whereAnalysis)
    {
        _whereAnalysis = whereAnalysis;
    }

    /// <summary>
    /// 检查字段是否有固定值
    /// </summary>
    private bool TryGetFixedValue(string propertyName, out object? fixedValue)
    {
        if (_whereAnalysis != null && _whereAnalysis.FixedValues.TryGetValue(propertyName, out var value))
        {
            fixedValue = value;
            return true;
        }
        fixedValue = null;
        return false;
    }

    /// <summary>
    /// 自动映射 Select（所有同名属性）
    /// </summary>
    public SelectParseResult ParseAuto<TSource, TResult>()
    {
        var result = new SelectParseResult
        {
            ResultType = typeof(TResult)
        };

        var sourceEntityInfo = EntityInfo.Get<TSource>();
        var targetType = typeof(TResult);

        // 获取目标类型的所有可写属性
        var targetProperties = targetType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
            .ToArray();

        var selectColumns = new List<string>();

        foreach (var targetProp in targetProperties)
        {
            // 检查 Where 中是否有该字段的固定值
            if (_whereAnalysis != null && _whereAnalysis.FixedValues.TryGetValue(targetProp.Name, out var fixedValue))
            {
                // 字段值已固定，不需要查询，但需要记录投影信息（用于填充固定值）
                result.Projections.Add(new SelectProjection
                {
                    SourceColumn = null,  // 不查询
                    TargetProperty = targetProp.Name,
                    TargetType = targetProp.PropertyType,
                    FixedValue = fixedValue  // 使用固定值
                });
                continue;  // 不添加到 SELECT 子句
            }

            // 查找源实体中同名的列
            var sourceColumn = sourceEntityInfo.Columns.FirstOrDefault(c => c.PropertyName == targetProp.Name);
            if (sourceColumn != null)
            {
                result.Projections.Add(new SelectProjection
                {
                    SourceColumn = sourceColumn.ColumnName,
                    TargetProperty = targetProp.Name,
                    TargetType = targetProp.PropertyType
                });
                selectColumns.Add($"{_provider.QuoteIdentifier(sourceColumn.ColumnName)} AS {_provider.QuoteIdentifier(targetProp.Name)}");
            }
        }

        // 检测是否所有字段都是固定值
        result.AllFieldsAreFixed = result.Projections.All(p => p.SourceColumn == null);

        // 如果所有字段都是固定值，不需要查询任何列，只需要获取记录数
        if (result.AllFieldsAreFixed)
        {
            result.SelectClause = "1";  // 只查询一个常量值用于确定记录数
        }
        else
        {
            result.SelectClause = selectColumns.Count > 0 ? string.Join(", ", selectColumns) : "*";
        }

        return result;
    }

    /// <summary>
    /// 解析 Select 表达式
    /// </summary>
    public SelectParseResult Parse<TSource, TResult>(Expression<Func<TSource, TResult>> selector)
    {
        var result = new SelectParseResult
        {
            ResultType = typeof(TResult)
        };

        // 处理单字段选择：it => it.Id
        if (selector.Body is MemberExpression memberExpr && memberExpr.Expression?.NodeType == ExpressionType.Parameter)
        {
            var entityInfo = EntityInfo.Get<TSource>();
            var propertyName = memberExpr.Member.Name;
            var column = entityInfo.Columns.FirstOrDefault(c => c.PropertyName == propertyName);

            result.Projections.Add(new SelectProjection
            {
                SourceColumn = column?.ColumnName ?? propertyName.ToLowerInvariant(),
                TargetProperty = propertyName,
                TargetType = typeof(TResult)
            });
            result.SelectClause = _provider.QuoteIdentifier(result.Projections[0].SourceColumn);
            return result;
        }

        // 处理类型转换：it => (object)it.Id
        if (selector.Body is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression memberExpr2)
        {
            var entityInfo = EntityInfo.Get<TSource>();
            var propertyName = memberExpr2.Member.Name;
            var column = entityInfo.Columns.FirstOrDefault(c => c.PropertyName == propertyName);

            result.Projections.Add(new SelectProjection
            {
                SourceColumn = column?.ColumnName ?? propertyName.ToLowerInvariant(),
                TargetProperty = propertyName,
                TargetType = typeof(TResult)
            });
            result.SelectClause = _provider.QuoteIdentifier(result.Projections[0].SourceColumn);
            return result;
        }

        // 处理成员初始化：it => new { Id = it.Id, Name = it.Name }
        if (selector.Body is MemberInitExpression memberInitExpr)
        {
            var entityInfo = EntityInfo.Get<TSource>();
            var selectColumns = new List<string>();

            foreach (var binding in memberInitExpr.Bindings)
            {
                if (binding is MemberAssignment assignment)
                {
                    var propertyName = binding.Member.Name;
                    var propertyInfo = binding.Member as PropertyInfo;
                    var propertyType = propertyInfo?.PropertyType ?? typeof(object);

                    // 检查是否有固定值
                    if (TryGetFixedValue(propertyName, out var fixedValue))
                    {
                        result.Projections.Add(new SelectProjection
                        {
                            SourceColumn = null,
                            TargetProperty = propertyName,
                            TargetType = propertyType,
                            FixedValue = fixedValue
                        });
                        continue;  // 不添加到 SELECT 子句
                    }

                    // 解析源属性：it.Id
                    string sourceColumn = string.Empty;
                    if (assignment.Expression is MemberExpression sourceMember && sourceMember.Expression?.NodeType == ExpressionType.Parameter)
                    {
                        var sourcePropName = sourceMember.Member.Name;
                        var column = entityInfo.Columns.FirstOrDefault(c => c.PropertyName == sourcePropName);
                        sourceColumn = column?.ColumnName ?? sourcePropName.ToLowerInvariant();
                    }

                    if (!string.IsNullOrEmpty(sourceColumn))
                    {
                        result.Projections.Add(new SelectProjection
                        {
                            SourceColumn = sourceColumn,
                            TargetProperty = propertyName,
                            TargetType = propertyType
                        });
                        selectColumns.Add($"{_provider.QuoteIdentifier(sourceColumn)} AS {_provider.QuoteIdentifier(propertyName)}");
                    }
                }
            }

            // 检测是否所有字段都是固定值
            result.AllFieldsAreFixed = result.Projections.All(p => p.SourceColumn == null);

            // 如果所有字段都是固定值，只查询一个常量值用于确定记录数
            if (result.AllFieldsAreFixed)
            {
                result.SelectClause = "1";
            }
            else
            {
                result.SelectClause = selectColumns.Count > 0 ? string.Join(", ", selectColumns) : "*";
            }

            return result;
        }

        // 处理匿名对象初始化：it => new { it.Id, it.Name }
        if (selector.Body is NewExpression newExpr && newExpr.Members != null)
        {
            var entityInfo = EntityInfo.Get<TSource>();
            var selectColumns = new List<string>();

            for (int i = 0; i < newExpr.Arguments.Count; i++)
            {
                var arg = newExpr.Arguments[i];
                var member = newExpr.Members[i];
                var propertyName = member.Name;
                var propertyInfo = member as PropertyInfo;
                var propertyType = propertyInfo?.PropertyType ?? typeof(object);

                // 检查是否有固定值
                if (TryGetFixedValue(propertyName, out var fixedValue))
                {
                    result.Projections.Add(new SelectProjection
                    {
                        SourceColumn = null,
                        TargetProperty = propertyName,
                        TargetType = propertyType,
                        FixedValue = fixedValue
                    });
                    continue;  // 不添加到 SELECT 子句
                }

                string sourceColumn = string.Empty;
                if (arg is MemberExpression sourceMember && sourceMember.Expression?.NodeType == ExpressionType.Parameter)
                {
                    var sourcePropName = sourceMember.Member.Name;
                    var column = entityInfo.Columns.FirstOrDefault(c => c.PropertyName == sourcePropName);
                    sourceColumn = column?.ColumnName ?? sourcePropName.ToLowerInvariant();
                }

                if (!string.IsNullOrEmpty(sourceColumn))
                {
                    result.Projections.Add(new SelectProjection
                    {
                        SourceColumn = sourceColumn,
                        TargetProperty = propertyName,
                        TargetType = propertyType
                    });
                    selectColumns.Add($"{_provider.QuoteIdentifier(sourceColumn)} AS {_provider.QuoteIdentifier(propertyName)}");
                }
            }

            // 检测是否所有字段都是固定值
            result.AllFieldsAreFixed = result.Projections.All(p => p.SourceColumn == null);

            // 如果所有字段都是固定值，只查询一个常量值用于确定记录数
            if (result.AllFieldsAreFixed)
            {
                result.SelectClause = "1";
            }
            else
            {
                result.SelectClause = selectColumns.Count > 0 ? string.Join(", ", selectColumns) : "*";
            }

            return result;
        }

        // 默认：返回所有列
        result.SelectClause = "*";
        return result;
    }
}

/// <summary>
/// Select 结果映射器
/// </summary>
public sealed class SelectMapper
{
    private static readonly ConcurrentDictionary<Type, Func<IDataReader, string, object>> _singleValueGetters = new();

    /// <summary>
    /// 映射单字段值
    /// </summary>
    public static TResult MapSingleValue<TResult>(IDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return default!;
        }

        var value = reader.GetValue(ordinal);
        return (TResult)Convert.ChangeType(value, typeof(TResult));
    }

    /// <summary>
    /// 映射到匿名对象或 DTO（暂时使用反射）
    /// </summary>
    public static TResult MapToType<TResult>(IDataReader reader, SelectParseResult selectResult)
    {
        var resultType = typeof(TResult);

        // 检查是否有无参构造函数
        var constructor = resultType.GetConstructor(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null);

        if (constructor != null)
        {
            // 有无参构造函数，使用 Activator.CreateInstance
            var result = Activator.CreateInstance<TResult>();

            foreach (var projection in selectResult.Projections)
            {
                var property = resultType.GetProperty(projection.TargetProperty);
                if (property != null && property.CanWrite)
                {
                    // 检查是否有固定值
                    if (projection.FixedValue != null)
                    {
                        // 使用 Where 中的固定值，不从数据库读取
                        var convertedValue = Convert.ChangeType(projection.FixedValue, property.PropertyType);
                        property.SetValue(result, convertedValue);
                    }
                    else if (projection.SourceColumn != null)
                    {
                        // 从数据库读取
                        var ordinal = reader.GetOrdinal(projection.TargetProperty);
                        if (!reader.IsDBNull(ordinal))
                        {
                            var value = reader.GetValue(ordinal);
                            var convertedValue = Convert.ChangeType(value, property.PropertyType);
                            property.SetValue(result, convertedValue);
                        }
                    }
                }
            }

            return result;
        }
        else
        {
            // 无无参构造函数（匿名类型），需要使用带参数的构造函数
            var values = new object[selectResult.Projections.Count];
            for (int i = 0; i < selectResult.Projections.Count; i++)
            {
                var projection = selectResult.Projections[i];

                // 检查是否有固定值
                if (projection.FixedValue != null)
                {
                    values[i] = Convert.ChangeType(projection.FixedValue, projection.TargetType);
                }
                else if (projection.SourceColumn != null)
                {
                    var ordinal = reader.GetOrdinal(projection.TargetProperty);
                    if (!reader.IsDBNull(ordinal))
                    {
                        var value = reader.GetValue(ordinal);
                        values[i] = Convert.ChangeType(value, projection.TargetType);
                    }
                    else
                    {
                        values[i] = null!;
                    }
                }
                else
                {
                    values[i] = null!;
                }
            }

            return (TResult)Activator.CreateInstance(resultType, values);
        }
    }

    /// <summary>
    /// 不使用 DataReader 直接映射（所有字段都是固定值）
    /// </summary>
    public static TResult MapToTypeWithoutReader<TResult>(SelectParseResult selectResult)
    {
        var resultType = typeof(TResult);

        // 检查是否有无参构造函数
        var constructor = resultType.GetConstructor(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null);

        if (constructor != null)
        {
            // 有无参构造函数，使用 Activator.CreateInstance
            var result = Activator.CreateInstance<TResult>();

            foreach (var projection in selectResult.Projections)
            {
                var property = resultType.GetProperty(projection.TargetProperty);
                if (property != null && property.CanWrite && projection.FixedValue != null)
                {
                    var convertedValue = Convert.ChangeType(projection.FixedValue, property.PropertyType);
                    property.SetValue(result, convertedValue);
                }
            }

            return result;
        }
        else
        {
            // 无无参构造函数（匿名类型），需要使用带参数的构造函数
            var values = new object[selectResult.Projections.Count];
            for (int i = 0; i < selectResult.Projections.Count; i++)
            {
                var projection = selectResult.Projections[i];
                if (projection.FixedValue != null)
                {
                    values[i] = Convert.ChangeType(projection.FixedValue, projection.TargetType);
                }
                else
                {
                    values[i] = null!;
                }
            }

            return (TResult)Activator.CreateInstance(resultType, values);
        }
    }

    /// <summary>
    /// 映射到动态对象
    /// </summary>
    public static object MapToDynamic(IDataReader reader, SelectParseResult selectResult)
    {
        var result = new System.Dynamic.ExpandoObject();
        var dict = (IDictionary<string, object>)result;

        foreach (var projection in selectResult.Projections)
        {
            var ordinal = reader.GetOrdinal(projection.SourceColumn);
            if (!reader.IsDBNull(ordinal))
            {
                var value = reader.GetValue(ordinal);
                dict[projection.TargetProperty] = value;
            }
            else
            {
                dict[projection.TargetProperty] = null!;
            }
        }

        return result;
    }
}
