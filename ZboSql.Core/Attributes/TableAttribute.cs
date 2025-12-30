namespace ZboSql.Core.Attributes;

/// <summary>
/// 标记实体类对应的数据库表
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute : Attribute
{
    /// <summary>
    /// 表名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 数据库 Schema（可选）
    /// </summary>
    public string? Schema { get; set; }

    public TableAttribute(string name)
    {
        Name = name;
    }
}
