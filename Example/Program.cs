using ZboSql.Core.Attributes;
using ZboSql.Core.Infrastructure;
using ZboSql.PostgreSql;

// 定义实体
[Table("test_users")]
public class TestUser
{
    [Column("id", IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? Email { get; set; }
}

// DTO 类示例
public class UserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
}

// 使用示例
public class Program
{
    private static readonly string ConnectionString =
        "Host=localhost;Port=5432;Database=sonar;Username=postgres;Password=azbo1020";

    public static async Task Main()
    {
        // 方式1: 使用 DbConfig 配置 SQL 打印（类似 SqlSugar）
        var config = DbConfig.Create()
            .SetConnectionString(ConnectionString)
            .SetDbType(DbType.PostgreSql)
            .SetPrintSql(true)  // 启用 SQL 打印
            .SetAutoCloseConnection(true)
            .Build();  // 构建配置

        var db = new ZboSqlClient(config);

        Console.WriteLine("=== ZboSql ORM 测试 ===\n");
        Console.WriteLine("测试表: test_users");
        Console.WriteLine("如需创建测试表，请执行:");
        Console.WriteLine("CREATE TABLE test_users (id SERIAL PRIMARY KEY, user_name VARCHAR(100), email VARCHAR(200));");
        Console.WriteLine();

        try
        {
            // 1. 查询所有用户
            Console.WriteLine("1. 查询所有用户:");
            var allUsers = await db.Queryable<TestUser>()
                .ToListAsync();
            foreach (var user in allUsers)
            {
                Console.WriteLine($"  ID: {user.Id}, UserName: {user.UserName}, Email: {user.Email}");
            }
            Console.WriteLine($"  总计: {allUsers.Count} 条记录\n");

            // 2. 条件查询
            Console.WriteLine("2. 条件查询 (Id > 0):");
            var filteredUsers = await db.Queryable<TestUser>()
                .Where(it => it.Id > 0)
                .OrderBy(it => it.Id)
                .Take(5)
                .ToListAsync();
            foreach (var user in filteredUsers)
            {
                Console.WriteLine($"  ID: {user.Id}, UserName: {user.UserName}");
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
            Console.WriteLine($"  新用户已插入，ID: {newUser.Id}\n");

            // 5. 更新用户
            Console.WriteLine("5. 更新用户:");
            newUser.Email = "updated@example.com";
            await db.Updateable(newUser)
                .Where(it => it.Id == newUser.Id)
                .ExecuteAsync();
            Console.WriteLine($"  用户 {newUser.Id} 已更新\n");

            // 6. 删除测试用户
            Console.WriteLine("6. 删除测试用户:");
            await db.Deleteable<TestUser>()
                .Where(it => it.Id == newUser.Id)
                .ExecuteAsync();
            Console.WriteLine($"  用户 {newUser.Id} 已删除\n");

            Console.WriteLine("=== 测试完成 ===");

            // 方式2: 使用 OnLogExecuting 事件进行自定义日志记录
            Console.WriteLine("\n=== 演示自定义日志事件 ===");
            var config2 = DbConfig.Create()
                .SetConnectionString(ConnectionString)
                .ConfigureAction(args =>
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[自定义日志] SQL: {args.Sql}");
                    Console.WriteLine($"[自定义日志] 参数数量: {args.Parameters.Length}");
                    Console.ResetColor();
                })
                .Build();  // 构建配置

            using var db2 = new ZboSqlClient(config2);
            var users = await db2.Queryable<TestUser>()
                .Where(it => it.Id > 0)
                .ToListAsync();
            Console.WriteLine($"查询到 {users.Count} 条记录");

            // ===== Select 功能演示 =====
            Console.WriteLine("\n=== Select 功能演示 ===");

            // Select 单字段
            Console.WriteLine("1. Select 单字段 (UserNames):");
            var userNames = await db.Queryable<TestUser>()
                .Select(it => it.UserName)
                .ToListAsync();
            foreach (var name in userNames)
            {
                Console.Write($"  {name}");
            }
            Console.WriteLine($"\n  共 {userNames.Count} 个\n");

            // Select DTO 类
            Console.WriteLine("2. Select DTO 类:");
            var userDtos = await db.Queryable<TestUser>()
                .Where(it => it.Id > 0)
                .Select(it => new UserDto { Id = it.Id, UserName = it.UserName })
                .ToListAsync();
            foreach (var dto in userDtos)
            {
                Console.WriteLine($"  Id: {dto.Id}, UserName: {dto.UserName}");
            }
            Console.WriteLine();

            // Select 多字段（带别名）
            Console.WriteLine("3. Select 带别名 (DTO):");
            var aliasedUsers = await db.Queryable<TestUser>()
                .Where(it => it.Id > 0)
                .Select(it => new UserDto { Id = it.Id, UserName = it.UserName })
                .ToListAsync();
            foreach (var user in aliasedUsers)
            {
                Console.WriteLine($"  Id: {user.Id}, UserName: {user.UserName}");
            }
            Console.WriteLine();

            // Select 自动映射（.Select<UserDto>()）
            Console.WriteLine("3.1. Select 自动映射 (.Select<UserDto>()):");
            var autoMappedUsers = await db.Queryable<TestUser>()
                .Where(it => it.Id > 0)
                .Select<UserDto>()
                .ToListAsync();
            foreach (var user in autoMappedUsers)
            {
                Console.WriteLine($"  Id: {user.Id}, UserName: {user.UserName}");
            }
            Console.WriteLine();

            // Select Id 列表
            Console.WriteLine("4. Select Id 列表:");
            var userIds = await db.Queryable<TestUser>()
                .Where(it => it.Id > 0)
                .Select(it => it.Id)
                .ToListAsync();
            Console.WriteLine($"  IDs: {string.Join(", ", userIds)}\n");

            // Select 匿名对象
            Console.WriteLine("5. Select 匿名对象:");
            Console.WriteLine("  测试 .Select(it => new { it.Email, it.UserName })");
            try
            {
                // 匿名对象 Select 需要用 var 接收
                var anonQuery = db.Queryable<TestUser>()
                    .Where(it => it.Id > 0)
                    .Select(it => new { it.Email, it.UserName });

                // 调用 ToListAsync
                var toListAsyncMethod = anonQuery.GetType().GetMethod("ToListAsync");
                if (toListAsyncMethod != null)
                {
                    var task = (Task)toListAsyncMethod.Invoke(anonQuery, null);
                    await task;
                    var resultProp = task.GetType().GetProperty("Result");
                    var anonList = resultProp?.GetValue(task);

                    if (anonList != null)
                    {
                        var list = (System.Collections.IList)anonList;
                        Console.WriteLine($"  成功! 返回 {list.Count} 条匿名对象记录");
                        if (list.Count > 0)
                        {
                            var first = list[0];
                            var emailProp = first.GetType().GetProperty("Email");
                            var nameProp = first.GetType().GetProperty("UserName");
                            Console.WriteLine($"  第一条: Email={emailProp?.GetValue(first)}, UserName={nameProp?.GetValue(first)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  错误: {ex.Message}");
            }
            Console.WriteLine();

            // ===== 投影优化演示 =====
            Console.WriteLine("\n=== 投影优化演示 ===");

            // 测试1: Where 中有固定值（AND 连接的等值条件）
            Console.WriteLine("1. Where 包含固定值 (x.UserName == \"zzz\"):");
            Console.WriteLine("   .Where(x => x.UserName == \"zzz\").Select<UserDto>().ToListAsync()");
            Console.WriteLine("   期望: SELECT 只查询 Id，不查询 user_name（使用固定值 \"zzz\"）");
            try
            {
                var query1 = db.Queryable<TestUser>()
                    .Where(x => x.UserName == "user2" || x.UserName == "user3@example.com")
                    .Where(x => x.Email == "user2@example.com")
                    .Select<TestUser>();

                var result1 = await query1.ToListAsync();

                Console.WriteLine($"   查询到 {result1.Count} 条记录");
                if (result1.Count > 0)
                {
                    var first = result1[0];
                    Console.WriteLine($"   第一条: Id={first.Id}, UserName={first.UserName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  错误: {ex.Message}");
            }
            Console.WriteLine();

            // 测试2: Where 中有 OR 条件（不优化）
            Console.WriteLine("2. Where 包含 OR 条件 (x.UserName == \"zzz\" || x.UserName == \"bbb\"):");
            Console.WriteLine("   .Where(x => x.UserName == \"zzz\" || x.UserName == \"bbb\").Select(x => new { x.UserName, x.Email })");
            Console.WriteLine("   期望: SELECT 查询 email 和 user_name（OR 条件不优化）");
            try
            {
                var optQuery2 = db.Queryable<TestUser>()
                    .Where(x => x.UserName == "zzz" || x.UserName == "bbb")
                    .Select(x => new { x.UserName, x.Email });

                var toListMethod2 = optQuery2.GetType().GetMethod("ToListAsync");
                if (toListMethod2 != null)
                {
                    var task2 = (Task)toListMethod2.Invoke(optQuery2, null);
                    await task2;
                    var resultProp2 = task2.GetType().GetProperty("Result");
                    var list2 = resultProp2?.GetValue(task2) as System.Collections.IList;

                    if (list2 != null)
                    {
                        Console.WriteLine($"   查询到 {list2.Count} 条记录");
                        if (list2.Count > 0)
                        {
                            var first = list2[0];
                            var emailProp = first.GetType().GetProperty("Email");
                            var nameProp = first.GetType().GetProperty("UserName");
                            var userName = nameProp?.GetValue(first);
                            var email = emailProp?.GetValue(first);
                            Console.WriteLine($"   第一条: UserName={userName} (数据库值), Email={email}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  错误: {ex.Message}");
            }
            Console.WriteLine();

            // 测试3: 多个 AND 条件（部分固定值）
            Console.WriteLine("3. Where 包含多个 AND 条件 (x.Id > 0 && x.UserName == \"admin\"):");
            Console.WriteLine("   .Where(x => x.Id > 0 && x.UserName == \"admin\").Select(x => new { x.UserName, x.Email })");
            Console.WriteLine("   期望: SELECT 只查询 email，UserName 使用固定值 \"admin\"（部分优化）");
            try
            {
                var optQuery3 = db.Queryable<TestUser>()
                    .Where(x => x.Id > 0 && x.UserName == "admin")
                    .Select(x => new { x.UserName, x.Email });

                var toListMethod3 = optQuery3.GetType().GetMethod("ToListAsync");
                if (toListMethod3 != null)
                {
                    var task3 = (Task)toListMethod3.Invoke(optQuery3, null);
                    await task3;
                    var resultProp3 = task3.GetType().GetProperty("Result");
                    var list3 = resultProp3?.GetValue(task3) as System.Collections.IList;

                    if (list3 != null)
                    {
                        Console.WriteLine($"   查询到 {list3.Count} 条记录");
                        if (list3.Count > 0)
                        {
                            var first = list3[0];
                            var emailProp = first.GetType().GetProperty("Email");
                            var nameProp = first.GetType().GetProperty("UserName");
                            var userName = nameProp?.GetValue(first);
                            var email = emailProp?.GetValue(first);
                            Console.WriteLine($"   第一条: UserName={userName}, Email={email}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  错误: {ex.Message}");
            }
            // 测试4: 多个固定值条件
            Console.WriteLine("4. Where 包含多个固定值 (x.UserName == \"zzz\" && x.Email == \"23@qq.com\"):");
            Console.WriteLine("   .Where(x => x.UserName == \"zzz\" && x.Email == \"23@qq.com\").Select(x => new { x.UserName, x.Email })");
            Console.WriteLine("   期望: SELECT 不查询任何字段（都使用固定值）");
            try
            {
                var optQuery4 = db.Queryable<TestUser>()
                    .Where(x => x.UserName == "zzz" && x.Email == "23@qq.com")
                    .Select(x => new { x.UserName, x.Email });

                var toListMethod4 = optQuery4.GetType().GetMethod("ToListAsync");
                if (toListMethod4 != null)
                {
                    var task4 = (Task)toListMethod4.Invoke(optQuery4, null);
                    await task4;
                    var resultProp4 = task4.GetType().GetProperty("Result");
                    var list4 = resultProp4?.GetValue(task4) as System.Collections.IList;

                    if (list4 != null)
                    {
                        Console.WriteLine($"   查询到 {list4.Count} 条记录");
                        if (list4.Count > 0)
                        {
                            var first = list4[0];
                            var emailProp = first.GetType().GetProperty("Email");
                            var nameProp = first.GetType().GetProperty("UserName");
                            var userName = nameProp?.GetValue(first);
                            var email = emailProp?.GetValue(first);
                            Console.WriteLine($"   第一条: UserName={userName} (固定值), Email={email} (固定值)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  错误: {ex.Message}");
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"错误: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("\n请确保数据库中存在 test_users 表");
        }
    }
}
