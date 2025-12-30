using System.Runtime.CompilerServices;

namespace ZboSql.Core.Infrastructure;

/// <summary>
/// 数据库配置选项
/// </summary>
public sealed class DbConfig
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DbType DbType { get; set; } = DbType.PostgreSql;

    /// <summary>
    /// 是否自动关闭连接
    /// </summary>
    public bool IsAutoCloseConnection { get; set; } = true;

    /// <summary>
    /// 是否打印 SQL
    /// </summary>
    public bool IsPrintSql { get; set; } = false;

    /// <summary>
    /// SQL 执行日志事件
    /// </summary>
    public Action<SqlLogEventArgs>? OnLogExecuting { get; set; }

    /// <summary>
    /// 配置构建器
    /// </summary>
    public sealed class Builder
    {
        private readonly DbConfig _config = new();

        /// <summary>
        /// 设置连接字符串
        /// </summary>
        public Builder SetConnectionString(string connectionString)
        {
            _config.ConnectionString = connectionString;
            return this;
        }

        /// <summary>
        /// 设置数据库类型
        /// </summary>
        public Builder SetDbType(DbType dbType)
        {
            _config.DbType = dbType;
            return this;
        }

        /// <summary>
        /// 设置是否自动关闭连接
        /// </summary>
        public Builder SetAutoCloseConnection(bool autoClose = true)
        {
            _config.IsAutoCloseConnection = autoClose;
            return this;
        }

        /// <summary>
        /// 设置是否打印 SQL
        /// </summary>
        public Builder SetPrintSql(bool printSql = true)
        {
            _config.IsPrintSql = printSql;
            return this;
        }

        /// <summary>
        /// 配置 SQL 日志事件
        /// </summary>
        public Builder ConfigureAction(Action<SqlLogEventArgs> onLogExecuting)
        {
            _config.OnLogExecuting = onLogExecuting;
            return this;
        }

        /// <summary>
        /// 构建配置
        /// </summary>
        public DbConfig Build()
        {
            return _config;
        }
    }

    /// <summary>
    /// 创建配置构建器
    /// </summary>
    public static Builder Create()
    {
        return new Builder();
    }
}
