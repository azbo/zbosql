using System.Runtime.CompilerServices;
using ZboSql.Core.Infrastructure;

namespace ZboSql.PostgreSql;

/// <summary>
/// ZboSql 客户端入口
/// </summary>
public sealed class ZboSqlClient : IDisposable
{
    private readonly NpgsqlProvider _provider;

    /// <summary>
    /// 创建 PostgreSQL 客户端
    /// </summary>
    public ZboSqlClient(string connectionString)
    {
        _provider = new NpgsqlProvider();
        _provider.SetConnectionString(connectionString);
    }

    /// <summary>
    /// 创建 PostgreSQL 客户端（带配置）
    /// </summary>
    public ZboSqlClient(DbConfig config)
    {
        _provider = new NpgsqlProvider();
        _provider.SetConfig(config);
    }

    /// <summary>
    /// 创建查询对象
    /// </summary>
    public Queryable<TSource> Queryable<TSource>(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        where TSource : class, new()
    {
        var sourceInfo = new SourceInfo(memberName, filePath, lineNumber);
        return new Queryable<TSource>(_provider, sourceInfo);
    }

    /// <summary>
    /// 创建插入对象
    /// </summary>
    public Insertable<TSource> Insertable<TSource>(TSource entity,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        where TSource : class
    {
        var sourceInfo = new SourceInfo(memberName, filePath, lineNumber);
        return new Insertable<TSource>(_provider, sourceInfo, entity);
    }

    /// <summary>
    /// 创建更新对象
    /// </summary>
    public Updateable<TSource> Updateable<TSource>(TSource entity,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        where TSource : class
    {
        var sourceInfo = new SourceInfo(memberName, filePath, lineNumber);
        return new Updateable<TSource>(_provider, sourceInfo, entity);
    }

    /// <summary>
    /// 创建删除对象
    /// </summary>
    public Deleteable<TSource> Deleteable<TSource>(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        where TSource : class
    {
        var sourceInfo = new SourceInfo(memberName, filePath, lineNumber);
        return new Deleteable<TSource>(_provider, sourceInfo);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _provider?.Dispose();
    }
}
