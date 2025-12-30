using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace ZboSql.Core.Infrastructure;

/// <summary>
/// 数据库提供程序抽象基类
/// </summary>
public abstract class DbProviderBase : IDbProvider
{
    protected string ConnectionString { get; private set; } = string.Empty;
    protected DbConnection? Connection { get; private set; }
    private DbConfig? _config;

    /// <summary>
    /// 数据库名称
    /// </summary>
    public abstract string DbName { get; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public virtual DbType DbType => DbType.PostgreSql;

    /// <summary>
    /// 配置选项
    /// </summary>
    public DbConfig Config
    {
        get => _config ??= new DbConfig();
        set => _config = value;
    }

    /// <summary>
    /// 引号标识符（如 Postgres: ", MySQL: `, SQL Server: [）
    /// </summary>
    protected abstract string QuoteChar { get; }

    /// <summary>
    /// 参数前缀（如 @）
    /// </summary>
    protected abstract string ParameterPrefix { get; }

    /// <summary>
    /// 设置连接字符串
    /// </summary>
    public void SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    /// 设置配置
    /// </summary>
    public void SetConfig(DbConfig config)
    {
        _config = config;
        ConnectionString = config.ConnectionString;
    }

    /// <summary>
    /// 获取数据库连接
    /// </summary>
    public DbConnection GetConnection()
    {
        if (Connection == null || Connection.State == ConnectionState.Closed)
        {
            Connection = CreateConnection();
            Connection.Open();
        }
        return Connection;
    }

    /// <summary>
    /// 创建数据库连接（由子类实现）
    /// </summary>
    protected abstract DbConnection CreateConnection();

    /// <summary>
    /// 引用标识符（如 "table_name"）
    /// </summary>
    public string QuoteIdentifier(string identifier)
    {
        return $"{QuoteChar}{identifier}{QuoteChar}";
    }

    /// <summary>
    /// 格式化参数名（如 @p0）
    /// </summary>
    public string FormatParameterName(string name)
    {
        return $"{ParameterPrefix}{name}";
    }

    /// <summary>
    /// 构建 LIMIT/OFFSET 子句
    /// </summary>
    public abstract string BuildLimitClause(int? skip, int? take);

    /// <summary>
    /// 创建命令
    /// </summary>
    public DbCommand CreateCommand(string sql, DbParameter[]? parameters = null)
    {
        var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;

        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        // 触发 SQL 日志事件
        OnLogExecuting(sql, parameters);

        return command;
    }

    /// <summary>
    /// 触发 SQL 日志事件
    /// </summary>
    protected virtual void OnLogExecuting(string sql, DbParameter[]? parameters)
    {
        if (Config.OnLogExecuting != null || Config.IsPrintSql)
        {
            var stopwatch = Stopwatch.StartNew();

            // 构建事件参数
            var eventArgs = new SqlLogEventArgs
            {
                Sql = sql,
                Parameters = parameters?.Select(p => p.Value).ToArray() ?? Array.Empty<object?>(),
                DbType = DbType
            };

            // 触发用户配置的日志事件
            Config.OnLogExecuting?.Invoke(eventArgs);

            // 如果启用了打印 SQL
            if (Config.IsPrintSql)
            {
                PrintSql(sql, parameters);
            }
        }
    }

    /// <summary>
    /// 打印 SQL 到控制台
    /// </summary>
    protected virtual void PrintSql(string sql, DbParameter[]? parameters)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SQL:");
        Console.ResetColor();

        Console.WriteLine(sql);

        if (parameters != null && parameters.Length > 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Parameters:");
            foreach (var param in parameters)
            {
                Console.WriteLine($"  {param.ParameterName} = {param.Value}");
            }
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public virtual void Dispose()
    {
        if (Config.IsAutoCloseConnection)
        {
            Connection?.Dispose();
            Connection = null;
        }
    }
}
