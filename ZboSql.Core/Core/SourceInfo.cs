using System.Runtime.CompilerServices;

namespace ZboSql.Core.Infrastructure;

/// <summary>
/// SQL 执行来源信息（调用栈追踪）
/// </summary>
public readonly struct SourceInfo
{
    /// <summary>
    /// 调用方法名
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// 源文件路径
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// 源代码行号
    /// </summary>
    public int LineNumber { get; }

    public SourceInfo(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        MemberName = memberName;
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// 生成 SQL 注释格式的来源信息
    /// 格式: /* Program.cs:25 Main() */
    /// </summary>
    public string ToComment()
    {
        var fileName = string.IsNullOrEmpty(FilePath) ? "unknown" : System.IO.Path.GetFileName(FilePath);
        return $"/* {fileName}:{LineNumber} {MemberName}() */";
    }
}
