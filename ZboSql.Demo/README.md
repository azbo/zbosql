# ZboSql.Demo - Blazor 产品管理系统

基于 ZboSql ORM 的 Blazor Server 产品管理系统演示。

## 功能特性

✅ **CRUD 操作**
- 新增产品
- 编辑产品
- 删除产品
- 查询产品列表

✅ **高级查询**
- 关键词搜索（产品名称）
- 分类筛选
- 多字段排序（名称、价格、库存、创建时间）
- 分页显示

✅ **数据验证**
- 输入验证
- 错误提示
- 确认对话框

## 技术栈

- **前端**: Blazor Server (.NET 9.0)
- **ORM**: ZboSql (自制轻量级 ORM)
- **数据库**: PostgreSQL
- **UI**: Bootstrap 5

## 快速开始

### 1. 创建数据库

```bash
# 连接到 PostgreSQL
psql -U postgres

# 执行初始化脚本
\i Scripts/InitDatabase.sql
```

或手动执行 SQL:

```sql
CREATE DATABASE zbosql_demo;

\c zbosql_demo;

CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    product_name VARCHAR(200) NOT NULL,
    price DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    stock_quantity INTEGER NOT NULL DEFAULT 0,
    category VARCHAR(100),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
```

### 2. 修改连接字符串

编辑 `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=zbosql_demo;Username=postgres;Password=your_password"
  }
}
```

### 3. 运行项目

```bash
cd ZboSql.Demo
dotnet run
```

访问: https://localhost:5001

## 使用说明

### 产品列表

- **搜索**: 在搜索框输入产品名称关键词
- **筛选**: 选择产品分类进行筛选
- **排序**: 选择排序字段（名称、价格、库存、创建时间）
- **分页**: 使用上一页/下一页按钮切换页面

### 新增产品

1. 点击"新增产品"按钮
2. 填写产品信息：
   - 产品名称（必填）
   - 价格（必填）
   - 库存数量（必填）
   - 分类（选填）
3. 点击"保存"

### 编辑产品

1. 点击产品行的"编辑"按钮
2. 修改产品信息
3. 点击"保存"

### 删除产品

1. 点击产品行的"删除"按钮
2. 确认删除操作

## 项目结构

```
ZboSql.Demo/
├── Components/
│   ├── Pages/
│   │   └── Products.razor        # 产品管理页面
│   └── Layout/
│       └── NavMenu.razor         # 导航菜单
├── Models/
│   └── Product.cs                # 产品实体
├── Services/
│   └── ProductService.cs         # 产品服务
├── Scripts/
│   └── InitDatabase.sql         # 数据库初始化脚本
├── Program.cs                    # 程序入口
└── appsettings.json             # 配置文件
```

## 数据库表结构

### products 表

| 字段 | 类型 | 说明 |
|------|------|------|
| id | SERIAL | 主键 |
| product_name | VARCHAR(200) | 产品名称 |
| price | DECIMAL(10,2) | 价格 |
| stock_quantity | INTEGER | 库存数量 |
| category | VARCHAR(100) | 分类 |
| created_at | TIMESTAMP | 创建时间 |
| updated_at | TIMESTAMP | 更新时间 |

## 开发说明

### ZboSql ORM 特性展示

本项目演示了 ZboSql ORM 的以下特性：

1. **基础查询**: `.Where()`, `.ToListAsync()`
2. **分页查询**: `.ToPageListWithCountAsync()`
3. **排序**: `.Asc()`, `.Desc()`
4. **模糊查询**: `.Contains()`
5. **CRUD 操作**: `Insertable`, `Updateable`, `Deleteable`
6. **命名映射**: PascalCase → snake_case 自动转换

示例代码：

```csharp
// 查询（带搜索和分页）
var result = await db.Queryable<Product>()
    .Where(x => x.ProductName.Contains(keyword))
    .OrderBy(x => x.Price)
    .ToPageListWithCountAsync(pageNumber, pageSize);

// 新增
await db.Insertable(product).ExecuteAsync();

// 更新
await db.Updateable(product)
    .Where(x => x.Id == id)
    .ExecuteAsync();

// 删除
await db.Deleteable<Product>()
    .Where(x => x.Id == id)
    .ExecuteAsync();
```

## License

MIT License
