using System.Data.Common;

namespace ZboSql.Core.Infrastructure;

/// <summary>
/// 数据库提供程序接口
/// </summary>
public interface IDbProvider : IDisposable
{
    /// <summary>
    /// 数据库名称
    /// </summary>
    string DbName { get; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    DbType DbType { get; }

    /// <summary>
    /// 配置选项
    /// </summary>
    DbConfig Config { get; }

    /// <summary>
    /// 获取数据库连接
    /// </summary>
    DbConnection GetConnection();

    /// <summary>
    /// 引用标识符（如 "table_name", `table_name`, [table_name]）
    /// </summary>
    string QuoteIdentifier(string identifier);

    /// <summary>
    /// 格式化参数名（如 @p0）
    /// </summary>
    string FormatParameterName(string name);

    /// <summary>
    /// 构建 LIMIT/OFFSET 子句
    /// </summary>
    string BuildLimitClause(int? skip, int? take);

    /// <summary>
    /// 创建命令
    /// </summary>
    DbCommand CreateCommand(string sql, DbParameter[]? parameters = null);
}
