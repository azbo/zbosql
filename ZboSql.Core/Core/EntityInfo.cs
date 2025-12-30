using System.Collections.Concurrent;
using System.Reflection;
using ZboSql.Core.Attributes;

namespace ZboSql.Core.Infrastructure;

/// <summary>
/// 命名转换工具
/// </summary>
internal static class NamingConverter
{
    /// <summary>
    /// 将 PascalCase 转换为 snake_case
    /// 例如: UserName → user_name, EmailAddress → email_address, ID → id
    /// </summary>
    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var ch = name[i];
            if (char.IsUpper(ch))
            {
                // 不是第一个字符，且前一个字符不是大写，则插入下划线
                if (i > 0 && !char.IsUpper(name[i - 1]))
                {
                    result.Append('_');
                }
                // 不是第一个字符，且后一个字符不是大写，则插入下划线（处理 ID → i_d 的情况）
                else if (i > 0 && i < name.Length - 1 && !char.IsUpper(name[i + 1]))
                {
                    result.Append('_');
                }
                result.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                result.Append(ch);
            }
        }
        return result.ToString();
    }
}

/// <summary>
/// 列信息描述
/// </summary>
public sealed class ColumnInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public Type PropertyType { get; set; } = typeof(object);
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsNullable { get; set; }
    public PropertyInfo PropertyInfo { get; set; } = null!;
}

/// <summary>
/// 实体信息描述（表映射）
/// </summary>
public sealed class EntityInfo
{
    public Type EntityType { get; set; } = null!;
    public string TableName { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public ColumnInfo[] Columns { get; set; } = null!;
    public ColumnInfo? PrimaryKey { get; set; }

    /// <summary>
    /// 实体缓存
    /// </summary>
    private static readonly ConcurrentDictionary<Type, EntityInfo> s_cache = new();

    /// <summary>
    /// 获取实体信息
    /// </summary>
    public static EntityInfo Get<T>() => Get(typeof(T));

    /// <summary>
    /// 获取实体信息
    /// </summary>
    public static EntityInfo Get(Type type)
    {
        return s_cache.GetOrAdd(type, t => Create(t));
    }

    /// <summary>
    /// 创建实体信息
    /// </summary>
    private static EntityInfo Create(Type type)
    {
        // 获取 Table 特性
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        var tableName = tableAttr?.Name ?? NamingConverter.ToSnakeCase(type.Name);
        var schema = tableAttr?.Schema;

        // 获取所有属性
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = new List<ColumnInfo>();

        ColumnInfo? primaryKey = null;

        foreach (var prop in properties)
        {
            // 跳过忽略的属性
            if (prop.IsDefined(typeof(IgnoreAttribute)))
                continue;

            // 无法写入的属性跳过
            if (prop.GetSetMethod() == null)
                continue;

            // 获取列名
            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var pkAttr = prop.GetCustomAttribute<PrimaryKeyAttribute>();

            var columnName = columnAttr?.Name ?? NamingConverter.ToSnakeCase(prop.Name);
            var isPrimaryKey = columnAttr?.IsPrimaryKey ?? pkAttr != null;
            var isIdentity = columnAttr?.IsIdentity ?? pkAttr?.IsIdentity ?? false;
            var isNullable = columnAttr?.IsNullable ?? Nullable.GetUnderlyingType(prop.PropertyType) != null || !prop.PropertyType.IsValueType;

            var columnInfo = new ColumnInfo
            {
                PropertyName = prop.Name,
                ColumnName = columnName,
                PropertyType = prop.PropertyType,
                IsPrimaryKey = isPrimaryKey,
                IsIdentity = isIdentity,
                IsNullable = isNullable,
                PropertyInfo = prop
            };

            columns.Add(columnInfo);

            if (isPrimaryKey)
            {
                primaryKey = columnInfo;
            }
        }

        return new EntityInfo
        {
            EntityType = type,
            TableName = tableName,
            Schema = schema,
            Columns = columns.ToArray(),
            PrimaryKey = primaryKey
        };
    }
}
