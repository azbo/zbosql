using ZboSql.Core.Attributes;
using ZboSql.Core.Infrastructure;
using ZboSql.PostgreSql;

[Table("test_users")]
public class TestUser
{
    [Column("id", IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    [Column("user_name")]
    public string UserName { get; set; } = string.Empty;
    [Column("email")]
    public string? Email { get; set; }
}

public class Program
{
    private static readonly string ConnectionString = "Host=localhost;Port=5432;Database=sonar;Username=postgres;Password=azbo1020";

    public static async Task Main()
    {
        var config = DbConfig.Create()
            .SetConnectionString(ConnectionString)
            .SetDbType(DbType.PostgreSql)
            .SetPrintSql(true)
            .SetAutoCloseConnection(true)
            .Build();

        var db = new ZboSqlClient(config);

        Console.WriteLine("测试匿名对象 Select: new { it.Email, it.UserName }");
        
        var anonQuery = db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Select(it => new { it.Email, it.UserName });

        Console.WriteLine($"查询类型: {anonQuery.GetType().FullName}");
        Console.WriteLine("ToListAsync 方法存在: " + (anonQuery.GetType().GetMethod("ToListAsync") != null));

        var toListAsyncMethod = anonQuery.GetType().GetMethod("ToListAsync");
        if (toListAsyncMethod != null)
        {
            var task = (Task)toListAsyncMethod.Invoke(anonQuery, null);
            await task;
            var resultProp = task.GetType().GetProperty("Result");
            var list = resultProp?.GetValue(task);
            Console.WriteLine($"结果类型: {list?.GetType().FullName}");
        }
    }
}
