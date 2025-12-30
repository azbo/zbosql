namespace ZboSql.Core.Attributes;

/// <summary>
/// 标记忽略的属性（不映射到数据库）
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class IgnoreAttribute : Attribute
{
}
