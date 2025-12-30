using System.Linq.Expressions;
using ZboSql.Core.Expressions;

namespace ZboSql.Core.Infrastructure;

/// <summary>
/// SQL 构建器接口
/// </summary>
public interface ISqlBuilder
{
    /// <summary>
    /// 构建 SELECT 语句
    /// </summary>
    string BuildSelect<TSource>(
        Expression<Func<TSource, bool>>? whereExpression,
        int? skip,
        int? take,
        SourceInfo sourceInfo);

    /// <summary>
    /// 构建 COUNT 语句
    /// </summary>
    string BuildCount<TSource>(
        Expression<Func<TSource, bool>>? whereExpression,
        SourceInfo sourceInfo);

    /// <summary>
    /// 构建 INSERT 语句
    /// </summary>
    string BuildInsert<TSource>(SourceInfo sourceInfo);

    /// <summary>
    /// 构建 UPDATE 语句
    /// </summary>
    string BuildUpdate<TSource>(
        Expression<Func<TSource, bool>> whereExpression,
        SourceInfo sourceInfo);

    /// <summary>
    /// 构建 DELETE 语句
    /// </summary>
    string BuildDelete<TSource>(
        Expression<Func<TSource, bool>> whereExpression,
        SourceInfo sourceInfo);
}
