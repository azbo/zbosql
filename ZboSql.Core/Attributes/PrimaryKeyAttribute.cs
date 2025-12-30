namespace ZboSql.Core.Attributes;

/// <summary>
/// 标记主键属性（简化版，使用属性名作为列名）
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PrimaryKeyAttribute : Attribute
{
    /// <summary>
    /// 是否为自增主键
    /// </summary>
    public bool IsIdentity { get; set; }
}
