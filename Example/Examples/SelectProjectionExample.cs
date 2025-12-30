namespace Example.Examples;

using Example.Models;
using ZboSql.PostgreSql;

/// <summary>
/// Select 投影查询示例
/// </summary>
public static class SelectProjectionExample
{
    public static async Task RunAsync(ZboSqlClient db)
    {
        Console.WriteLine("=== Select 投影查询 ===\n");

        // 1. Select 单字段
        Console.WriteLine("1. Select 单字段 (UserName):");
        var userNames = await db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Select(it => it.UserName)
            .ToListAsync();
        Console.WriteLine($"   查询到 {userNames.Count} 个用户名");
        foreach (var name in userNames.Take(5))
        {
            Console.Write($"   {name}");
        }
        Console.WriteLine("\n");

        // 2. Select Id 列表
        Console.WriteLine("2. Select Id 列表:");
        var userIds = await db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Select(it => it.Id)
            .ToListAsync();
        Console.WriteLine($"   IDs: {string.Join(", ", userIds.Take(5))}\n");

        // 3. Select DTO 类（手动映射）
        Console.WriteLine("3. Select DTO 类（手动映射）:");
        var userDtos = await db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Select(it => new UserDto { Id = it.Id, UserName = it.UserName })
            .ToListAsync();
        Console.WriteLine($"   查询到 {userDtos.Count} 条 DTO 记录");
        foreach (var dto in userDtos.Take(3))
        {
            Console.WriteLine($"   Id: {dto.Id}, UserName: {dto.UserName}");
        }
        Console.WriteLine();

        // 4. Select 自动映射（.Select<TDto>()）
        Console.WriteLine("4. Select 自动映射 (.Select<UserDto>()):");
        var autoMappedUsers = await db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Select<UserDto>()
            .ToListAsync();
        Console.WriteLine($"   查询到 {autoMappedUsers.Count} 条记录（自动映射同名属性）");
        foreach (var user in autoMappedUsers.Take(3))
        {
            Console.WriteLine($"   Id: {user.Id}, UserName: {user.UserName}");
        }
        Console.WriteLine();

        // 5. Select 多字段 DTO
        Console.WriteLine("5. Select 多字段 DTO:");
        var detailDtos = await db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Select(it => new UserDetailDto { Id = it.Id, UserName = it.UserName, Email = it.Email })
            .ToListAsync();
        Console.WriteLine($"   查询到 {detailDtos.Count} 条详情记录");
        foreach (var dto in detailDtos.Take(3))
        {
            Console.WriteLine($"   Id: {dto.Id}, UserName: {dto.UserName}, Email: {dto.Email}");
        }
        Console.WriteLine();

        Console.WriteLine("=== Select 投影查询完成 ===\n");
    }
}
