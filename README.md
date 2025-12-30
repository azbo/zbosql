# ZboSql ORM

一个轻量级的 .NET ORM 框架，支持 Fluent API 链式调用和 SQL 来源追踪功能。

## 特性

- **简单易用**：直观的 Fluent API 设计
- **高性能**：实体映射缓存，避免重复反射开销
- **智能投影优化**：自动识别 Where 中的固定值，减少查询字段
- **来源追踪**：自动记录 SQL 调用的类名、方法名和行号
- **类型安全**：强类型查询，编译时检查
- **多目标框架**：支持 .NET 9.0 和 .NET Standard 2.1
- **多数据库支持**：核心库不依赖具体数据库，按需引用驱动

## 项目结构

```
ZboSql/
├── ZboSql.Core/              # 核心库（数据库无关）
│   ├── Attributes/           # Table, Column, PrimaryKey, Ignore
│   ├── Core/                 # IDbProvider, DbProviderBase, EntityInfo, SourceInfo
│   ├── Expressions/          # 表达式解析和优化
│   │   ├── ExpressionParser.cs      # 表达式解析器
│   │   ├── ExpressionOptimizer.cs   # OR to IN 优化
│   │   ├── SelectExpressionParser.cs # Select 投影解析
│   │   └── SelectProjectionOptimizer.cs # 投影优化器
│   └── Infrastructure/       # ISqlBuilder 等接口
│
├── ZboSql.PostgreSql/        # PostgreSQL 驱动
│   ├── NpgsqlProvider.cs     # PostgreSQL 提供程序实现
│   ├── PostgresSqlBuilder.cs # SQL 构建器
│   └── Api/                  # Queryable, Insertable, Updateable, Deleteable, ZboSqlClient
│       ├── Queryable.cs      # 查询 API（支持多 Where 合并、Select 投影）
│       ├── Insertable.cs     # 插入 API
│       ├── Updateable.cs     # 更新 API
│       └── Deleteable.cs     # 删除 API
│
└── Example/                  # 示例项目
    └── Program.cs            # 完整的 CRUD 和 Select 示例
```

## 快速开始

### 1. 安装包

```bash
# 核心库（必选）
dotnet add package ZboSql.Core

# PostgreSQL 驱动（根据需要选择）
dotnet add package ZboSql.PostgreSql
```

### 2. 定义实体

```csharp
using ZboSql.Core.Attributes;

[Table("test_users")]
public class TestUser
{
    [Column("id", IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [Column("username")]
    public string UserName { get; set; } = string.Empty;

    [Column("email")]
    public string? Email { get; set; }
}
```

### 3. 创建客户端

```csharp
using ZboSql.PostgreSql;

var db = new ZboSqlClient("Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password");
```

### 4. 查询操作

```csharp
// 列表查询
var users = await db.Queryable<TestUser>()
    .Where(it => it.Id > 10)
    .OrderBy(it => it.Id)
    .Skip(0)
    .Take(10)
    .ToListAsync();

// 第一条记录
var first = await db.Queryable<TestUser>()
    .Where(it => it.UserName == "admin")
    .FirstOrDefaultAsync();

// 计数
var count = await db.Queryable<TestUser>()
    .Where(it => it.Id > 0)
    .CountAsync();

// 多 Where 条件（自动合并）
var results = await db.Queryable<TestUser>()
    .Where(it => it.UserName == "admin")
    .Where(it => it.Email == "admin@example.com")
    .ToListAsync();
// 生成 SQL: WHERE (("username" = @p0) AND ("email" = @p1))
```

### 5. Select 投影

```csharp
// 自动映射 DTO
public class UserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
}

var userDtos = await db.Queryable<TestUser>()
    .Where(it => it.Id > 0)
    .Select<UserDto>()  // 自动映射同名属性
    .ToListAsync();

// 匿名对象投影
var anonUsers = await db.Queryable<TestUser>()
    .Where(it => it.Id > 0)
    .Select(it => new { it.Id, it.UserName })
    .ToListAsync();

// 单字段投影
var userIds = await db.Queryable<TestUser>()
    .Where(it => it.Id > 0)
    .Select(it => it.Id)
    .ToListAsync();
```

### 6. 智能投影优化

当 Where 条件中包含字段的固定值时，Select 会自动优化查询，不查询这些字段：

```csharp
// 示例 1: 单字段固定值
var results = await db.Queryable<TestUser>()
    .Where(it => it.UserName == "admin")
    .Select(it => new { it.UserName, it.Email })
    .ToListAsync();
// 生成 SQL: SELECT "email" FROM "test_users" WHERE ("username" = @p0)
// UserName 使用固定值 "admin"，不查询数据库

// 示例 2: 多字段固定值
var results = await db.Queryable<TestUser>()
    .Where(it => it.UserName == "admin" && it.Email == "admin@example.com")
    .Select(it => new { it.UserName, it.Email })
    .ToListAsync();
// 生成 SQL: SELECT 1 FROM "test_users" WHERE (("username" = @p0) AND ("email" = @p1))
// 所有字段都是固定值，只查询记录数，不查询字段值

// 示例 3: 部分固定值（混合条件）
var results = await db.Queryable<TestUser>()
    .Where(it => it.Id > 0 && it.UserName == "admin")
    .Select(it => new { it.UserName, it.Email })
    .ToListAsync();
// 生成 SQL: SELECT "email" FROM "test_users" WHERE (("id" > @p0) AND ("username" = @p1))
// UserName 使用固定值 "admin"，Email 仍需查询

// 示例 4: OR 条件（不优化）
var results = await db.Queryable<TestUser>()
    .Where(it => it.UserName == "admin" || it.UserName == "user")
    .Select(it => new { it.UserName, it.Email })
    .ToListAsync();
// 生成 SQL: SELECT "username", "email" FROM "test_users" WHERE ("username" IN (@p0, @p1))
// OR 条件不优化，查询所有字段
```

**优化规则**：
- ✅ 收集所有 AND 连接的等值条件（`==`）作为固定值
- ❌ 遇到 OR（`||`）时，清空所有固定值（OR 会导致字段值不确定）
- ⚠️ 其他运算符（`>`, `<`, `>=`, `<=`, `!=`）不影响固定值收集
- ✅ 如果 Select 字段在固定值字典中，使用固定值；否则查询数据库
- ✅ 如果**所有** Select 字段都是固定值，使用 `SELECT 1`，不查询任何字段值

### 7. 插入操作

```csharp
var newUser = new TestUser
{
    UserName = "test_user",
    Email = "test@example.com"
};
await db.Insertable(newUser).ExecuteAsync();
// newUser.Id 自动获取返回的 ID
```

### 8. 更新操作

```csharp
newUser.Email = "updated@example.com";
await db.Updateable(newUser)
    .Where(it => it.Id == newUser.Id)
    .ExecuteAsync();
```

### 9. 删除操作

```csharp
await db.Deleteable<TestUser>()
    .Where(it => it.Id < 10)
    .ExecuteAsync();
```

## 生成的 SQL 示例

所有 SQL 语句都会自动添加来源注释：

```sql
/* Program.cs:25 Main() */
SELECT "id", "username", "email"
FROM "test_users"
WHERE ("id" > @p0)
ORDER BY "id" ASC
LIMIT 10
```

## 支持的表达式

- **比较运算**：`>`, `<`, `=`, `!=`, `>=`, `<=`, `&&`, `||`
- **字符串方法**：`Contains`, `StartsWith`, `EndsWith`
- **排序**：`OrderBy`, `OrderByDescending`
- **分页**：`Skip`, `Take`
- **投影**：`Select<TResult>()`, `Select(selector)`
- **多条件合并**：链式调用 `.Where()` 自动使用 AND 合并条件

## 高级特性

### 智能投影优化

当 Select 的字段在 Where 条件中有固定值时，ORM 会自动优化 SQL 查询：

| Where 条件 | Select 字段 | 优化效果 |
|-----------|------------|---------|
| `x.UserName == "admin"` | UserName, Email | ✅ 只查 Email |
| `x.Id > 0 && x.UserName == "admin"` | UserName, Email | ✅ 只查 Email |
| `x.UserName == "a" \|\| x.UserName == "b"` | UserName, Email | ❌ 查询所有字段 |
| `x.UserName == "admin" && x.Email == "test@test.com"` | UserName, Email | ✅ SELECT 1（最优化） |

**性能优势**：
- 减少数据库数据传输量
- 降低网络开销
- 提升查询性能

## 扩展其他数据库

### 创建 MySQL 驱动

1. 创建项目 `ZboSql.MySql`
2. 引用 `ZboSql.Core`
3. 实现 `DbProviderBase`

```csharp
using MySqlConnector;
using ZboSql.Core.Infrastructure;

namespace ZboSql.MySql;

public sealed class MySqlProvider : DbProviderBase
{
    public override string DbName => "MySQL";
    protected override string QuoteChar => "`";
    protected override string ParameterPrefix => "@";

    protected override DbConnection CreateConnection() =>
        new MySqlConnection(ConnectionString);

    public override string BuildLimitClause(int? skip, int? take) =>
        $"LIMIT {skip},{take}";
}
```

4. 实现 SQL 构建器和 API

## 架构设计

### 核心库 (ZboSql.Core)
- **Attributes**：实体映射特性
- **Core**：抽象基类和接口
- **Expressions**：表达式解析器
- 不依赖任何具体数据库

### 数据库驱动 (ZboSql.PostgreSql)
- 继承 `DbProviderBase`
- 实现 `ISqlBuilder`
- 提供具体的 Fluent API

### 使用示例项目
- 完整的 CRUD 演示
- 测试表创建脚本

## 创建测试表

```sql
CREATE TABLE test_users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL,
    email VARCHAR(200)
);

INSERT INTO test_users (username, email) VALUES
('user1', 'user1@example.com'),
('user2', 'user2@example.com');
```

## 许可证

MIT License
