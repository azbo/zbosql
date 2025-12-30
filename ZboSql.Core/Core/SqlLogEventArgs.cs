namespace ZboSql.Core.Infrastructure;

/// <summary>
/// SQL 日志事件参数
/// </summary>
public sealed class SqlLogEventArgs
{
    /// <summary>
    /// 原始 SQL（带参数占位符）
    /// </summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// 参数值列表
    /// </summary>
    public object?[] Parameters { get; set; } = Array.Empty<object?>();

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DbType DbType { get; set; }
}
