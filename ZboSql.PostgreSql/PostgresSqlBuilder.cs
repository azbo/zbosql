using System.Text;
using System.Linq.Expressions;
using ZboSql.Core.Infrastructure;
using ZboSql.Core.Expressions;

namespace ZboSql.PostgreSql;

/// <summary>
/// PostgreSQL SQL 构建器
/// </summary>
public sealed class PostgresSqlBuilder : ISqlBuilder
{
    private readonly IDbProvider _provider;

    public PostgresSqlBuilder(IDbProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// 构建 SELECT 语句
    /// </summary>
    public string BuildSelect<TSource>(
        Expression<Func<TSource, bool>>? whereExpression,
        int? skip,
        int? take,
        SourceInfo sourceInfo)
    {
        return BuildSelect(whereExpression, null, false, skip, take, sourceInfo);
    }

    /// <summary>
    /// 构建 SELECT 语句（带 OrderBy）
    /// </summary>
    public string BuildSelect<TSource>(
        Expression<Func<TSource, bool>>? whereExpression,
        Expression<Func<TSource, object>>? orderByExpression,
        bool orderByDescending,
        int? skip,
        int? take,
        SourceInfo sourceInfo,
        string customSelectClause = null!)
    {
        return BuildSelect(whereExpression, orderByExpression, orderByDescending, skip, take, sourceInfo, customSelectClause, typeof(TSource));
    }

    /// <summary>
    /// 构建 SELECT 语句（带 OrderBy，使用实体类型）
    /// </summary>
    public string BuildSelect<TSource>(
        Expression<Func<TSource, bool>>? whereExpression,
        Expression<Func<TSource, object>>? orderByExpression,
        bool orderByDescending,
        int? skip,
        int? take,
        SourceInfo sourceInfo,
        string customSelectClause,
        Type entityType)  // 指定实体类型
    {
        return BuildSelect(whereExpression as LambdaExpression, orderByExpression as LambdaExpression, orderByDescending, skip, take, sourceInfo, customSelectClause, entityType);
    }

    /// <summary>
    /// 构建 SELECT 语句（使用 LambdaExpression）
    /// </summary>
    public string BuildSelect(
        LambdaExpression? whereExpression,
        LambdaExpression? orderByExpression,
        bool orderByDescending,
        int? skip,
        int? take,
        SourceInfo sourceInfo,
        string customSelectClause,
        Type entityType)
    {
        var entityInfo = EntityInfo.Get(entityType);
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(entityType, whereExpression);

        var sql = new StringBuilder();

        // 来源注释
        if (!string.IsNullOrEmpty(sourceInfo.ToComment()))
        {
            sql.AppendLine(sourceInfo.ToComment());
        }

        // SELECT 子句
        sql.Append("SELECT ");

        // 使用自定义 SELECT 子句或默认所有列
        if (!string.IsNullOrEmpty(customSelectClause))
        {
            sql.Append(customSelectClause);
        }
        else
        {
            var columns = string.Join(", ", entityInfo.Columns.Select(c =>
                _provider.QuoteIdentifier(c.ColumnName)));
            sql.Append(columns);
        }

        // FROM 子句
        sql.AppendLine();
        sql.Append("FROM ");
        if (!string.IsNullOrEmpty(entityInfo.Schema))
        {
            sql.Append(_provider.QuoteIdentifier(entityInfo.Schema)).Append('.');
        }
        sql.Append(_provider.QuoteIdentifier(entityInfo.TableName));

        // WHERE 子句
        if (!string.IsNullOrEmpty(whereResult.Sql))
        {
            sql.AppendLine().Append("WHERE ").Append(whereResult.Sql);
        }

        // ORDER BY 子句
        if (orderByExpression != null)
        {
            sql.AppendLine();
            var propertyName = GetPropertyName(orderByExpression);
            var column = entityInfo.Columns.FirstOrDefault(c => c.PropertyName == propertyName);
            var columnName = column?.ColumnName ?? propertyName.ToLowerInvariant();
            sql.Append("ORDER BY ")
                .Append(_provider.QuoteIdentifier(columnName))
                .Append(orderByDescending ? " DESC" : " ASC");
        }

        // LIMIT/OFFSET 子句
        var limitClause = _provider.BuildLimitClause(skip, take);
        if (!string.IsNullOrEmpty(limitClause))
        {
            sql.AppendLine().Append(limitClause);
        }

        return sql.ToString();
    }

    /// <summary>
    /// 从表达式中提取属性名
    /// </summary>
    private static string GetPropertyName<TSource, TKey>(Expression<Func<TSource, TKey>> expression)
    {
        return GetPropertyName((LambdaExpression)expression);
    }

    /// <summary>
    /// 从表达式中提取属性名（使用 LambdaExpression）
    /// </summary>
    private static string GetPropertyName(LambdaExpression expression)
    {
        if (expression.Body is MemberExpression memberExp)
        {
            return memberExp.Member.Name;
        }
        if (expression.Body is UnaryExpression unaryExp && unaryExp.Operand is MemberExpression memberExp2)
        {
            return memberExp2.Member.Name;
        }
        throw new InvalidOperationException("Unable to extract property name from expression");
    }

    /// <summary>
    /// 构建 COUNT 语句
    /// </summary>
    public string BuildCount<TSource>(
        Expression<Func<TSource, bool>>? whereExpression,
        SourceInfo sourceInfo)
    {
        return BuildCount(whereExpression, sourceInfo, typeof(TSource));
    }

    /// <summary>
    /// 构建 COUNT 语句（使用 LambdaExpression 和指定类型）
    /// </summary>
    public string BuildCount(
        LambdaExpression? whereExpression,
        SourceInfo sourceInfo,
        Type entityType)
    {
        var entityInfo = EntityInfo.Get(entityType);
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(entityType, whereExpression);

        var sql = new StringBuilder();

        // 来源注释
        if (!string.IsNullOrEmpty(sourceInfo.ToComment()))
        {
            sql.AppendLine(sourceInfo.ToComment());
        }

        sql.Append("SELECT COUNT(*) FROM ");
        if (!string.IsNullOrEmpty(entityInfo.Schema))
        {
            sql.Append(_provider.QuoteIdentifier(entityInfo.Schema)).Append('.');
        }
        sql.Append(_provider.QuoteIdentifier(entityInfo.TableName));

        if (!string.IsNullOrEmpty(whereResult.Sql))
        {
            sql.AppendLine().Append("WHERE ").Append(whereResult.Sql);
        }

        return sql.ToString();
    }

    /// <summary>
    /// 构建 INSERT 语句
    /// </summary>
    public string BuildInsert<TSource>(SourceInfo sourceInfo)
    {
        var entityInfo = EntityInfo.Get<TSource>();
        var columns = entityInfo.Columns.Where(c => !c.IsIdentity).ToArray();

        var sql = new StringBuilder();

        // 来源注释
        if (!string.IsNullOrEmpty(sourceInfo.ToComment()))
        {
            sql.AppendLine(sourceInfo.ToComment());
        }

        sql.Append("INSERT INTO ");
        if (!string.IsNullOrEmpty(entityInfo.Schema))
        {
            sql.Append(_provider.QuoteIdentifier(entityInfo.Schema)).Append('.');
        }
        sql.Append(_provider.QuoteIdentifier(entityInfo.TableName));

        sql.Append(" (");
        sql.Append(string.Join(", ", columns.Select(c => _provider.QuoteIdentifier(c.ColumnName))));
        sql.Append(") VALUES (");
        sql.Append(string.Join(", ", columns.Select(c =>
            _provider.FormatParameterName(c.PropertyName))));
        sql.Append(')');

        // 返回自增主键
        if (entityInfo.PrimaryKey?.IsIdentity == true)
        {
            sql.Append(" RETURNING ").Append(_provider.QuoteIdentifier(entityInfo.PrimaryKey.ColumnName));
        }

        return sql.ToString();
    }

    /// <summary>
    /// 构建 UPDATE 语句
    /// </summary>
    public string BuildUpdate<TSource>(
        Expression<Func<TSource, bool>> whereExpression,
        SourceInfo sourceInfo)
    {
        var entityInfo = EntityInfo.Get<TSource>();
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(whereExpression);

        var sql = new StringBuilder();

        // 来源注释
        if (!string.IsNullOrEmpty(sourceInfo.ToComment()))
        {
            sql.AppendLine(sourceInfo.ToComment());
        }

        sql.Append("UPDATE ");
        if (!string.IsNullOrEmpty(entityInfo.Schema))
        {
            sql.Append(_provider.QuoteIdentifier(entityInfo.Schema)).Append('.');
        }
        sql.Append(_provider.QuoteIdentifier(entityInfo.TableName));

        // SET 子句（非主键列）
        var updateColumns = entityInfo.Columns.Where(c => !c.IsPrimaryKey).ToArray();
        sql.AppendLine().Append("SET ");
        sql.Append(string.Join(", ", updateColumns.Select(c =>
            $"{_provider.QuoteIdentifier(c.ColumnName)} = {_provider.FormatParameterName(c.PropertyName)}")));

        // WHERE 子句
        if (!string.IsNullOrEmpty(whereResult.Sql))
        {
            sql.AppendLine().Append("WHERE ").Append(whereResult.Sql);
        }
        else
        {
            throw new InvalidOperationException("UPDATE requires WHERE clause");
        }

        return sql.ToString();
    }

    /// <summary>
    /// 构建 DELETE 语句
    /// </summary>
    public string BuildDelete<TSource>(
        Expression<Func<TSource, bool>> whereExpression,
        SourceInfo sourceInfo)
    {
        var entityInfo = EntityInfo.Get<TSource>();
        var parser = new ExpressionParser(_provider);
        var whereResult = parser.ParseWhere(whereExpression);

        var sql = new StringBuilder();

        // 来源注释
        if (!string.IsNullOrEmpty(sourceInfo.ToComment()))
        {
            sql.AppendLine(sourceInfo.ToComment());
        }

        sql.Append("DELETE FROM ");
        if (!string.IsNullOrEmpty(entityInfo.Schema))
        {
            sql.Append(_provider.QuoteIdentifier(entityInfo.Schema)).Append('.');
        }
        sql.Append(_provider.QuoteIdentifier(entityInfo.TableName));

        // WHERE 子句
        if (!string.IsNullOrEmpty(whereResult.Sql))
        {
            sql.AppendLine().Append("WHERE ").Append(whereResult.Sql);
        }
        else
        {
            throw new InvalidOperationException("DELETE requires WHERE clause");
        }

        return sql.ToString();
    }
}
