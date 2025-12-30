using System.ComponentModel.DataAnnotations;
using ZboSql.Core.Attributes;

namespace ZboSql.Demo.Models;

/// <summary>
/// 产品实体
/// </summary>
[Table("products")]
public class Product
{
    [Column("id", IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [Column("product_name")]
    [Required(ErrorMessage = "产品名称不能为空")]
    [StringLength(200, ErrorMessage = "产品名称不能超过200个字符")]
    public string ProductName { get; set; } = string.Empty;

    [Column("price")]
    [Required(ErrorMessage = "价格不能为空")]
    [Range(0.01, 999999, ErrorMessage = "价格必须在0.01到999999之间")]
    public decimal Price { get; set; }

    [Column("stock_quantity")]
    [Required(ErrorMessage = "库存不能为空")]
    [Range(0, int.MaxValue, ErrorMessage = "库存不能为负数")]
    public int StockQuantity { get; set; }

    [Column("category")]
    [StringLength(100, ErrorMessage = "分类不能超过100个字符")]
    public string? Category { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
