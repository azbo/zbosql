namespace Example.Examples;

using Example.Models;
using ZboSql.PostgreSql;

/// <summary>
/// 字符串方法查询示例
/// </summary>
public static class StringMethodsExample
{
    public static async Task RunAsync(ZboSqlClient db)
    {
        Console.WriteLine("=== 字符串方法查询 ===\n");

        // 1. Contains - 包含字符串
        Console.WriteLine("1. Contains - 查询 UserName 包含 'user' 的记录:");
        var containsUsers = await db.Queryable<TestUser>()
            .Where(it => it.UserName.Contains("user"))
            .ToListAsync();
        Console.WriteLine($"   查询到 {containsUsers.Count} 条记录");
        foreach (var user in containsUsers.Take(3))
        {
            Console.WriteLine($"   UserName: {user.UserName}");
        }
        Console.WriteLine();

        // 2. StartsWith - 以字符串开头
        Console.WriteLine("2. StartsWith - 查询 UserName 以 'user' 开头的记录:");
        var startsWithUsers = await db.Queryable<TestUser>()
            .Where(it => it.UserName.StartsWith("user"))
            .ToListAsync();
        Console.WriteLine($"   查询到 {startsWithUsers.Count} 条记录");
        foreach (var user in startsWithUsers.Take(3))
        {
            Console.WriteLine($"   UserName: {user.UserName}");
        }
        Console.WriteLine();

        // 3. EndsWith - 以字符串结尾
        Console.WriteLine("3. EndsWith - 查询 Email 以 '@example.com' 结尾的记录:");
        var endsWithUsers = await db.Queryable<TestUser>()
            .Where(it => it.Email.EndsWith("@example.com"))
            .ToListAsync();
        Console.WriteLine($"   查询到 {endsWithUsers.Count} 条记录");
        foreach (var user in endsWithUsers.Take(3))
        {
            Console.WriteLine($"   Email: {user.Email}");
        }
        Console.WriteLine();

        // 4. 组合使用
        Console.WriteLine("4. 组合条件 - StartsWith + Contains:");
        var combinedUsers = await db.Queryable<TestUser>()
            .Where(it => it.UserName.StartsWith("user") && it.Email != null && it.Email.Contains("@example.com"))
            .ToListAsync();
        Console.WriteLine($"   查询到 {combinedUsers.Count} 条记录");
        Console.WriteLine();

        Console.WriteLine("=== 字符串方法查询完成 ===\n");
    }
}
