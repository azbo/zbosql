namespace Example.Models;

/// <summary>
/// 用户 DTO（数据传输对象）
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// 用户详情 DTO
/// </summary>
public class UserDetailDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
