using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;
using ZboSql.Core.Infrastructure;

namespace ZboSql.Core.Expressions;

/// <summary>
/// 高性能对象映射器（使用表达式树编译）
/// </summary>
public static class ObjectMapper
{
    private static readonly ConcurrentDictionary<Type, Func<IDataReader, object>> _readerMappers = new();
    private static readonly ConcurrentDictionary<string, object> _selectMappers = new();

    /// <summary>
    /// 从 IDataReader 映射到实体（使用表达式树）
    /// </summary>
    public static T MapFromReader<T>(IDataReader reader) where T : class, new()
    {
        var mapper = (Func<IDataReader, T>)GetOrCreateReaderMapper<T>();
        return mapper(reader);
    }

    /// <summary>
    /// 从 IDataReader 映射到实体（使用表达式树，动态类型）
    /// </summary>
    public static object MapFromReaderDynamic(IDataReader reader, Type type)
    {
        var mapper = (Func<IDataReader, object>)GetOrCreateReaderMapperDynamic(type);
        return mapper(reader);
    }

    /// <summary>
    /// Select 映射（使用表达式树）
    /// </summary>
    public static T MapSelect<T>(IDataReader reader, SelectParseResult selectResult)
    {
        // 使用 SelectClause 作为缓存键的一部分
        var cacheKey = $"{typeof(T).FullName}|{selectResult.SelectClause}";
        var mapperTyped = (Func<IDataReader, SelectParseResult, T>)_selectMappers.GetOrAdd(cacheKey, _ =>
        {
            return CompileSelectMapper<T>(selectResult);
        });

        return mapperTyped(reader, selectResult);
    }

    private static Func<IDataReader, T> GetOrCreateReaderMapper<T>() where T : class, new()
    {
        return (Func<IDataReader, T>)_readerMappers.GetOrAdd(typeof(T), _ =>
        {
            return CompileReaderMapper<T>();
        });
    }

    private static Func<IDataReader, object> GetOrCreateReaderMapperDynamic(Type type)
    {
        return _readerMappers.GetOrAdd(type, _ =>
        {
            return CompileReaderMapperDynamic(type);
        });
    }

    /// <summary>
    /// 编译 Reader 映射器（泛型版本）
    /// </summary>
    private static Func<IDataReader, T> CompileReaderMapper<T>() where T : class, new()
    {
        var readerParam = Expression.Parameter(typeof(IDataReader), "reader");
        var entityInfo = EntityInfo.Get<T>();
        var entityType = typeof(T);

        // 获取方法信息
        var getOrdinalMethod = typeof(IDataReader).GetMethod(nameof(IDataReader.GetOrdinal), new[] { typeof(string) });
        var isDbNullMethod = typeof(IDataReader).GetMethod(nameof(IDataReader.IsDBNull), new[] { typeof(int) });
        var getValueMethod = typeof(IDataReader).GetMethod(nameof(IDataReader.GetValue), new[] { typeof(int) });

        // var entity = new T();
        var newExpr = Expression.New(entityType);
        var entityVar = Expression.Variable(entityType, "entity");
        var assignments = new List<Expression>
        {
            Expression.Assign(entityVar, newExpr)
        };

        // foreach column: entity.Prop = (Type)reader.GetValue(i);
        foreach (var column in entityInfo.Columns)
        {
            var property = entityType.GetProperty(column.PropertyName);
            if (property == null || !property.CanWrite)
                continue;

            // var ordinal = reader.GetOrdinal(columnName);
            var ordinalVar = Expression.Variable(typeof(int), $"ordinal_{column.PropertyName}");
            var getOrdinalCall = Expression.Call(readerParam, getOrdinalMethod, Expression.Constant(column.ColumnName));

            // if (!reader.IsDBNull(ordinal))
            var isDbNullCall = Expression.Call(readerParam, isDbNullMethod, ordinalVar);

            var getValueCall = Expression.Call(readerParam, getValueMethod, ordinalVar);

            var convertCall = Expression.Convert(
                Expression.Call(
                    typeof(Convert),
                    nameof(Convert.ChangeType),
                    null,
                    getValueCall,
                    Expression.Constant(property.PropertyType)),
                property.PropertyType);

            var assignValue = Expression.Assign(
                Expression.Property(entityVar, property),
                convertCall);

            var ifBlock = Expression.IfThen(
                Expression.Not(isDbNullCall),
                assignValue);

            assignments.Add(Expression.Assign(ordinalVar, getOrdinalCall));
            assignments.Add(ifBlock);
        }

        assignments.Add(entityVar);

        var block = Expression.Block(new[] { entityVar }, assignments);
        var lambda = Expression.Lambda<Func<IDataReader, T>>(block, readerParam);
        return lambda.Compile();
    }

    /// <summary>
    /// 编译 Reader 映射器（动态版本）
    /// </summary>
    private static Func<IDataReader, object> CompileReaderMapperDynamic(Type entityType)
    {
        var readerParam = Expression.Parameter(typeof(IDataReader), "reader");
        var entityInfo = EntityInfo.Get(entityType);

        // 获取方法信息
        var getOrdinalMethod = typeof(IDataReader).GetMethod(nameof(IDataReader.GetOrdinal), new[] { typeof(string) });
        var isDbNullMethod = typeof(IDataReader).GetMethod(nameof(IDataReader.IsDBNull), new[] { typeof(int) });
        var getValueMethod = typeof(IDataReader).GetMethod(nameof(IDataReader.GetValue), new[] { typeof(int) });

        // var entity = new T();
        var newExpr = Expression.New(entityType);
        var entityVar = Expression.Variable(entityType, "entity");
        var assignments = new List<Expression>
        {
            Expression.Assign(entityVar, newExpr)
        };

        // foreach column: entity.Prop = (Type)reader.GetValue(i);
        foreach (var column in entityInfo.Columns)
        {
            var property = entityType.GetProperty(column.PropertyName);
            if (property == null || !property.CanWrite)
                continue;

            var ordinalVar = Expression.Variable(typeof(int), $"ordinal_{column.PropertyName}");
            var getOrdinalCall = Expression.Call(readerParam, getOrdinalMethod, Expression.Constant(column.ColumnName));

            var isDbNullCall = Expression.Call(readerParam, isDbNullMethod, ordinalVar);

            var getValueCall = Expression.Call(readerParam, getValueMethod, ordinalVar);

            var convertCall = Expression.Convert(
                Expression.Call(
                    typeof(Convert),
                    nameof(Convert.ChangeType),
                    null,
                    getValueCall,
                    Expression.Constant(property.PropertyType)),
                property.PropertyType);

            var assignValue = Expression.Assign(
                Expression.Property(entityVar, property),
                convertCall);

            var ifBlock = Expression.IfThen(
                Expression.Not(isDbNullCall),
                assignValue);

            assignments.Add(Expression.Assign(ordinalVar, getOrdinalCall));
            assignments.Add(ifBlock);
        }

        assignments.Add(Expression.Convert(entityVar, typeof(object)));

        var block = Expression.Block(new[] { entityVar }, assignments);
        var lambda = Expression.Lambda<Func<IDataReader, object>>(block, readerParam);
        return lambda.Compile();
    }

    /// <summary>
    /// 编译 Select 映射器
    /// </summary>
    private static Func<IDataReader, SelectParseResult, T> CompileSelectMapper<T>(SelectParseResult selectResult)
    {
        var readerParam = Expression.Parameter(typeof(IDataReader), "reader");
        var resultType = typeof(T);

        // 获取方法信息
        var getOrdinalMethod = typeof(IDataReader).GetMethod(nameof(IDataReader.GetOrdinal), new[] { typeof(string) });
        var getValueMethod = typeof(IDataReader).GetMethod(nameof(IDataReader.GetValue), new[] { typeof(int) });

        // 检查是否有无参构造函数
        var constructor = resultType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);

        if (constructor != null)
        {
            // 有无参构造函数：new T() { Prop1 = val1, Prop2 = val2 }
            var newExpr = Expression.New(resultType);
            var bindings = new List<MemberBinding>();

            foreach (var projection in selectResult.Projections)
            {
                var property = resultType.GetProperty(projection.TargetProperty);
                if (property == null || !property.CanWrite)
                    continue;

                // reader.GetOrdinal(projection.TargetProperty)
                var getOrdinalCall = Expression.Call(readerParam, getOrdinalMethod, Expression.Constant(projection.TargetProperty));

                // reader.GetValue(ordinal)
                var getValueCall = Expression.Call(readerParam, getValueMethod, getOrdinalCall);

                // (Type)Convert.ChangeType(value, targetType)
                var convertCall = Expression.Convert(
                    Expression.Call(
                        typeof(Convert),
                        nameof(Convert.ChangeType),
                        null,
                        getValueCall,
                        Expression.Constant(property.PropertyType)),
                    property.PropertyType);

                bindings.Add(Expression.Bind(property, convertCall));
            }

            var memberInit = Expression.MemberInit(newExpr, bindings);

            // 创建 Lambda 表达式
            var lambda = Expression.Lambda<Func<IDataReader, SelectParseResult, T>>(
                memberInit, readerParam, Expression.Parameter(typeof(SelectParseResult), "selectResult"));
            return lambda.Compile();
        }
        else
        {
            // 无无参构造函数（匿名类型）：new T(val1, val2, ...)
            var values = new List<Expression>();

            foreach (var projection in selectResult.Projections)
            {
                // reader.GetOrdinal(projection.TargetProperty)
                var getOrdinalCall = Expression.Call(readerParam, getOrdinalMethod, Expression.Constant(projection.TargetProperty));

                // reader.GetValue(ordinal)
                var getValueCall = Expression.Call(readerParam, getValueMethod, getOrdinalCall);

                // (Type)Convert.ChangeType(value, targetType)
                var convertCall = Expression.Convert(
                    Expression.Call(
                        typeof(Convert),
                        nameof(Convert.ChangeType),
                        null,
                        getValueCall,
                        Expression.Constant(projection.TargetType)),
                    projection.TargetType);

                values.Add(convertCall);
            }

            var ctor = resultType.GetConstructors()[0];
            var newExpr = Expression.New(ctor, values);

            // 创建 Lambda 表达式
            var lambda = Expression.Lambda<Func<IDataReader, SelectParseResult, T>>(
                newExpr, readerParam, Expression.Parameter(typeof(SelectParseResult), "selectResult"));
            return lambda.Compile();
        }
    }
}
