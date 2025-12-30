namespace Example.Models;

using ZboSql.Core.Attributes;

/// <summary>
/// 测试用户实体
/// </summary>
[Table("test_users")]
public class TestUser
{
    [Column("id", IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [Column("user_name")]
    public string UserName { get; set; } = string.Empty;

    [Column("email")]
    public string? Email { get; set; }
}
