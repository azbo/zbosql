using Example.Examples;
using Example.Models;
using ZboSql.Core.Infrastructure;
using ZboSql.PostgreSql;

/// <summary>
/// ZboSql ORM 示例程序
/// </summary>
public class Program
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=sonar;Username=postgres;Password=azbo1020";

    public static async Task Main()
    {
        // 创建数据库客户端
        var config = DbConfig.Create()
            .SetConnectionString(ConnectionString)
            .SetDbType(DbType.PostgreSql)
            .SetPrintSql(true)
            .SetAutoCloseConnection(true)
            .Build();

        using var db = new ZboSqlClient(config);

        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           ZboSql ORM - 功能演示                        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("测试表: test_users");
        Console.WriteLine("如需创建测试表，请执行:");
        Console.WriteLine("CREATE TABLE test_users (id SERIAL PRIMARY KEY, user_name VARCHAR(100), email VARCHAR(200));");
        Console.WriteLine();
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        try
        {
            // 菜单选项
            var examples = new Dictionary<int, Func<Task>>
            {
                { 1, () => BasicCrudExample.RunAsync(db) },
                { 2, () => StringMethodsExample.RunAsync(db) },
                { 3, () => SelectProjectionExample.RunAsync(db) },
                { 4, () => PaginationExample.RunAsync(db) },
                { 5, () => ProjectionOptimizationExample.RunAsync(db) },
                { 6, () => RunAllExamplesAsync(db) },
                { 0, () => Task.CompletedTask }
            };

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("请选择要运行的示例:");
                Console.WriteLine("  1. 基本 CRUD 操作");
                Console.WriteLine("  2. 字符串方法查询 (Contains/StartsWith/EndsWith)");
                Console.WriteLine("  3. Select 投影查询");
                Console.WriteLine("  4. 分页查询");
                Console.WriteLine("  5. Select 投影优化");
                Console.WriteLine("  6. 运行所有示例");
                Console.WriteLine("  0. 退出");
                Console.WriteLine();
                Console.Write("请输入选项 (0-6): ");

                var input = Console.ReadLine();
                if (!int.TryParse(input, out int choice) || !examples.ContainsKey(choice))
                {
                    Console.WriteLine("无效的选项，请重新输入。");
                    continue;
                }

                Console.WriteLine();

                if (choice == 0)
                {
                    Console.WriteLine("感谢使用 ZboSql ORM！");
                    break;
                }

                try
                {
                    await examples[choice]();
                    Console.WriteLine("✓ 示例执行完成");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ 执行失败: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"严重错误: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("请确保:");
            Console.WriteLine("1. PostgreSQL 数据库正在运行");
            Console.WriteLine("2. 数据库中存在 test_users 表");
            Console.WriteLine("3. 连接字符串配置正确");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 运行所有示例
    /// </summary>
    private static async Task RunAllExamplesAsync(ZboSqlClient db)
    {
        var examples = new List<(string Name, Func<Task> Example)>
        {
            ("基本 CRUD 操作", () => BasicCrudExample.RunAsync(db)),
            ("字符串方法查询", () => StringMethodsExample.RunAsync(db)),
            ("Select 投影查询", () => SelectProjectionExample.RunAsync(db)),
            ("分页查询", () => PaginationExample.RunAsync(db)),
            ("Select 投影优化", () => ProjectionOptimizationExample.RunAsync(db))
        };

        foreach (var (Name, Example) in examples)
        {
            try
            {
                Console.WriteLine($"\n{'━',60}");
                Console.WriteLine($"运行示例: {Name}");
                Console.WriteLine($"{'━',60}");
                await Example();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ {Name} 执行失败: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("\n所有示例执行完成！");
    }
}
