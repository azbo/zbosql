namespace Example.Examples;

using Example.Models;
using ZboSql.PostgreSql;

/// <summary>
/// 分页查询示例
/// </summary>
public static class PaginationExample
{
    public static async Task RunAsync(ZboSqlClient db)
    {
        Console.WriteLine("=== 分页查询示例 ===\n");

        int pageSize = 2;

        // 1. 基本分页（不返回总数）
        Console.WriteLine("1. 基本分页 - 第1页，每页2条:");
        var page1 = db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Asc(it => it.Id)
            .ToPageList(1, pageSize);
        Console.WriteLine($"   当前页记录数: {page1.Count}");
        foreach (var user in page1)
        {
            Console.WriteLine($"   Id: {user.Id}, UserName: {user.UserName}");
        }
        Console.WriteLine();

        // 2. 分页返回总记录数
        Console.WriteLine("2. 分页+总记录数 - 第1页，每页2条:");
        int totalCount = 0;
        var page2 = db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Asc(it => it.Id)
            .ToPageList(1, pageSize, ref totalCount);
        Console.WriteLine($"   当前页记录数: {page2.Count}");
        Console.WriteLine($"   总记录数: {totalCount}");
        foreach (var user in page2)
        {
            Console.WriteLine($"   Id: {user.Id}, UserName: {user.UserName}");
        }
        Console.WriteLine();

        // 3. 分页返回总记录数和总页数
        Console.WriteLine("3. 分页+总记录数+总页数 - 第1页，每页2条:");
        int totalCount3 = 0, totalPages = 0;
        var page3 = db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Asc(it => it.Id)
            .ToPageList(1, pageSize, ref totalCount3, ref totalPages);
        Console.WriteLine($"   当前页记录数: {page3.Count}");
        Console.WriteLine($"   总记录数: {totalCount3}");
        Console.WriteLine($"   总页数: {totalPages}");
        foreach (var user in page3)
        {
            Console.WriteLine($"   Id: {user.Id}, UserName: {user.UserName}");
        }
        Console.WriteLine();

        // 4. 异步分页（返回总数）
        Console.WriteLine("4. 异步分页+总记录数 - 第2页:");
        var result4 = await db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Asc(it => it.Id)
            .ToPageListWithCountAsync(2, pageSize);
        Console.WriteLine($"   当前页记录数: {result4.Items.Count}");
        Console.WriteLine($"   总记录数: {result4.TotalCount}");
        foreach (var user in result4.Items)
        {
            Console.WriteLine($"   Id: {user.Id}, UserName: {user.UserName}");
        }
        Console.WriteLine();

        // 5. 异步分页（返回总数和总页数）
        Console.WriteLine("5. 异步分页+总记录数+总页数 - 第2页:");
        var result5 = await db.Queryable<TestUser>()
            .Where(it => it.Id > 0)
            .Asc(it => it.Id)
            .ToPageListWithTotalPagesAsync(2, pageSize);
        Console.WriteLine($"   当前页记录数: {result5.Items.Count}");
        Console.WriteLine($"   总记录数: {result5.TotalCount}");
        Console.WriteLine($"   总页数: {result5.TotalPages}");
        foreach (var user in result5.Items)
        {
            Console.WriteLine($"   Id: {user.Id}, UserName: {user.UserName}");
        }
        Console.WriteLine();

        Console.WriteLine("=== 分页查询示例完成 ===\n");
    }
}
