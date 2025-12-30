namespace ZboSql.Core.Attributes;

/// <summary>
/// 标记属性对应的数据库列
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ColumnAttribute : Attribute
{
    /// <summary>
    /// 列名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 是否为主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否为自增列
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// 是否可为空
    /// </summary>
    public bool IsNullable { get; set; } = true;

    public ColumnAttribute(string name)
    {
        Name = name;
    }
}
