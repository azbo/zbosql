using ZboSql.Core.Infrastructure;
using ZboSql.Demo.Models;
using ZboSql.PostgreSql;

namespace ZboSql.Demo.Services;

/// <summary>
/// 产品服务
/// </summary>
public class ProductService
{
    private readonly ZboSqlClient _db;

    public ProductService(ZboSqlClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取产品列表（支持搜索和排序）
    /// </summary>
    public async Task<(List<Product> Products, int TotalCount)> GetProductsAsync(
        string? searchKeyword = null,
        string? category = null,
        string? orderBy = null,
        bool descending = false,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = _db.Queryable<Product>();

        // 搜索过滤
        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            query = query.Where(x => x.ProductName.Contains(searchKeyword));
        }

        // 分类过滤
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        // 排序
        if (!string.IsNullOrWhiteSpace(orderBy))
        {
            switch (orderBy.ToLower())
            {
                case "name":
                    query = descending ? query.Desc(x => x.ProductName) : query.Asc(x => x.ProductName);
                    break;
                case "price":
                    query = descending ? query.Desc(x => x.Price) : query.Asc(x => x.Price);
                    break;
                case "stock":
                    query = descending ? query.Desc(x => x.StockQuantity) : query.Asc(x => x.StockQuantity);
                    break;
                case "created":
                    query = descending ? query.Desc(x => x.CreatedAt) : query.Asc(x => x.CreatedAt);
                    break;
                default:
                    query = query.Asc(x => x.Id);
                    break;
            }
        }

        // 分页
        return await query.ToPageListWithCountAsync(pageNumber, pageSize);
    }

    /// <summary>
    /// 根据 ID 获取产品
    /// </summary>
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _db.Queryable<Product>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 创建产品
    /// </summary>
    public async Task<Product> CreateAsync(Product product)
    {
        product.CreatedAt = DateTime.Now;
        await _db.Insertable(product).ExecuteAsync();
        return product;
    }

    /// <summary>
    /// 更新产品
    /// </summary>
    public async Task<bool> UpdateAsync(Product product)
    {
        product.UpdatedAt = DateTime.Now;
        var rows = await _db.Updateable(product)
            .Where(x => x.Id == product.Id)
            .ExecuteAsync();
        return rows > 0;
    }

    /// <summary>
    /// 删除产品
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var rows = await _db.Deleteable<Product>()
            .Where(x => x.Id == id)
            .ExecuteAsync();
        return rows > 0;
    }

    /// <summary>
    /// 获取所有分类
    /// </summary>
    public async Task<List<string>> GetCategoriesAsync()
    {
        var products = await _db.Queryable<Product>()
            .Where(x => x.Category != null)
            .ToListAsync();

        return products
            .Select(x => x.Category!)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }
}
