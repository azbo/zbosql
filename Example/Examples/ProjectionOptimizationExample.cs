namespace Example.Examples;

using Example.Models;
using ZboSql.PostgreSql;

/// <summary>
/// Select 投影优化示例
/// </summary>
public static class ProjectionOptimizationExample
{
    public static async Task RunAsync(ZboSqlClient db)
    {
        Console.WriteLine("=== Select 投影优化示例 ===\n");
        Console.WriteLine("投影优化规则：当 Where 条件中包含字段的固定值时，Select 会自动优化 SQL 查询\n");

        // 1. 单字段固定值
        Console.WriteLine("1. 单字段固定值 (UserName == \"admin\"):");
        Console.WriteLine("   .Where(x => x.UserName == \"admin\").Select(x => new { x.UserName, x.Email })");
        Console.WriteLine("   期望: SELECT 只查询 email（UserName 使用固定值 \"admin\"）");
        try
        {
            var result1 = await db.Queryable<TestUser>()
                .Where(x => x.UserName == "admin")
                .Select(x => new { x.UserName, x.Email })
                .ToListAsync();
            Console.WriteLine($"   查询到 {result1.Count} 条记录");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   错误: {ex.Message}");
        }
        Console.WriteLine();

        // 2. 多字段固定值
        Console.WriteLine("2. 多字段固定值 (UserName && Email 都是固定值):");
        Console.WriteLine("   .Where(x => x.UserName == \"admin\" && x.Email == \"admin@test.com\")");
        Console.WriteLine("   .Select(x => new { x.UserName, x.Email })");
        Console.WriteLine("   期望: SELECT 1（所有字段都是固定值，只查询记录是否存在）");
        try
        {
            var result2 = await db.Queryable<TestUser>()
                .Where(x => x.UserName == "admin" && x.Email == "admin@test.com")
                .Select(x => new { x.UserName, x.Email })
                .ToListAsync();
            Console.WriteLine($"   查询到 {result2.Count} 条记录");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   错误: {ex.Message}");
        }
        Console.WriteLine();

        // 3. 部分固定值（混合条件）
        Console.WriteLine("3. 部分固定值 (Id > 0 && UserName == \"admin\"):");
        Console.WriteLine("   .Where(x => x.Id > 0 && x.UserName == \"admin\")");
        Console.WriteLine("   .Select(x => new { x.UserName, x.Email })");
        Console.WriteLine("   期望: SELECT 只查询 email（UserName 使用固定值 \"admin\"）");
        try
        {
            var result3 = await db.Queryable<TestUser>()
                .Where(x => x.Id > 0 && x.UserName == "admin")
                .Select(x => new { x.UserName, x.Email })
                .ToListAsync();
            Console.WriteLine($"   查询到 {result3.Count} 条记录");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   错误: {ex.Message}");
        }
        Console.WriteLine();

        // 4. OR 条件（不优化）
        Console.WriteLine("4. OR 条件 (UserName == \"admin\" || UserName == \"user\"):");
        Console.WriteLine("   .Where(x => x.UserName == \"admin\" || x.UserName == \"user\")");
        Console.WriteLine("   .Select(x => new { x.UserName, x.Email })");
        Console.WriteLine("   期望: SELECT 查询 user_name 和 email（OR 条件不优化）");
        try
        {
            var result4 = await db.Queryable<TestUser>()
                .Where(x => x.UserName == "admin" || x.UserName == "user")
                .Select(x => new { x.UserName, x.Email })
                .ToListAsync();
            Console.WriteLine($"   查询到 {result4.Count} 条记录");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   错误: {ex.Message}");
        }
        Console.WriteLine();

        // 5. 实际应用场景
        Console.WriteLine("5. 实际应用 - 根据用户名精确查询:");
        Console.WriteLine("   .Where(x => x.UserName == \"user2\")");
        Console.WriteLine("   .Select<UserDto>()");
        try
        {
            var result5 = await db.Queryable<TestUser>()
                .Where(x => x.UserName == "user2")
                .Select<UserDto>()
                .ToListAsync();
            Console.WriteLine($"   查询到 {result5.Count} 条记录");
            if (result5.Count > 0)
            {
                Console.WriteLine($"   第一条: Id={result5[0].Id}, UserName={result5[0].UserName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   错误: {ex.Message}");
        }
        Console.WriteLine();

        Console.WriteLine("=== 投影优化示例完成 ===\n");
    }
}
