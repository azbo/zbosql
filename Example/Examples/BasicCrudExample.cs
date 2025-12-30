namespace Example.Examples;

using Example.Models;
using ZboSql.PostgreSql;

/// <summary>
/// 基本 CRUD 操作示例
/// </summary>
public static class BasicCrudExample
{
    public static async Task RunAsync(ZboSqlClient db)
    {
        Console.WriteLine("=== 基本 CRUD 操作 ===\n");

        // 1. 查询所有用户
        Console.WriteLine("1. 查询所有用户:");
        var allUsers = await db.Queryable<TestUser>()
            .ToListAsync();
        foreach (var user in allUsers)
        {
            Console.WriteLine($"   ID: {user.Id}, UserName: {user.UserName}, Email: {user.Email}");
        }
        Console.WriteLine($"   总计: {allUsers.Count} 条记录\n");

        // 2. 条件查询
        Console.WriteLine("2. 条件查询 (Id > 0):");
        var filteredUsers = await db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Asc(it => it.Id)
            .Take(5)
            .ToListAsync();
        foreach (var user in filteredUsers)
        {
            Console.WriteLine($"   ID: {user.Id}, UserName: {user.UserName}");
        }
        Console.WriteLine();

        // 3. 计数
        var count = await db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .CountAsync();
        Console.WriteLine($"3. 用户总数: {count}\n");

        // 4. 插入新用户
        Console.WriteLine("4. 插入新用户:");
        var newUser = new TestUser
        {
            UserName = "test_user_" + DateTime.Now.Ticks.ToString().Substring(Math.Max(0, DateTime.Now.Ticks.ToString().Length - 8)),
            Email = "test@example.com"
        };
        await db.Insertable(newUser).ExecuteAsync();
        Console.WriteLine($"   新用户已插入，ID: {newUser.Id}\n");

        // 5. 更新用户
        Console.WriteLine("5. 更新用户:");
        newUser.Email = "updated@example.com";
        await db.Updateable(newUser)
            .Where(it => it.Id == newUser.Id)
            .ExecuteAsync();
        Console.WriteLine($"   用户 {newUser.Id} 已更新\n");

        // 6. 删除测试用户
        Console.WriteLine("6. 删除测试用户:");
        await db.Deleteable<TestUser>()
            .Where(it => it.Id == newUser.Id)
            .ExecuteAsync();
        Console.WriteLine($"   用户 {newUser.Id} 已删除\n");

        Console.WriteLine("=== CRUD 操作完成 ===\n");
    }
}
