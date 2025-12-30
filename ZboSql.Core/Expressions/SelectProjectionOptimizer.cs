using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace ZboSql.Core.Expressions;

/// <summary>
/// Where 条件分析结果
/// </summary>
public sealed class WhereAnalysis
{
    public Dictionary<string, object?> FixedValues { get; set; } = new();
    /// <summary>
    /// 出现在 OR 条件中的字段（值不确定，不能使用固定值优化）
    /// </summary>
    public HashSet<string> FieldsInOrConditions { get; set; } = new();
}

/// <summary>
/// Select 投影优化器
/// </summary>
public sealed class SelectProjectionOptimizer
{
    /// <summary>
    /// 分析 Where 表达式，提取字段的固定值
    /// </summary>
    public static WhereAnalysis AnalyzeWhere(LambdaExpression? whereExpression)
    {
        var analysis = new WhereAnalysis();
        if (whereExpression == null)
            return analysis;

        var analyzer = new WhereAnalyzer(analysis);
        analyzer.Visit(whereExpression);
        return analysis;
    }
}

/// <summary>
/// 字段名收集器（收集表达式中涉及的所有字段名）
/// </summary>
internal sealed class FieldNameCollector : ExpressionVisitor
{
    private readonly HashSet<string> _fieldNames;

    public FieldNameCollector(HashSet<string> fieldNames)
    {
        _fieldNames = fieldNames;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        Visit(node.Left);
        Visit(node.Right);
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // 只收集参数表达式的成员（x.FieldName）
        if (node.Expression?.NodeType == ExpressionType.Parameter)
        {
            _fieldNames.Add(node.Member.Name);
        }
        return node;
    }
}

/// <summary>
/// Where 表达式分析器
/// </summary>
internal sealed class WhereAnalyzer : ExpressionVisitor
{
    private readonly WhereAnalysis _analysis;

    public WhereAnalyzer(WhereAnalysis analysis)
    {
        _analysis = analysis;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // 处理 AND 条件
        if (node.NodeType == ExpressionType.AndAlso)
        {
            Visit(node.Left);
            Visit(node.Right);
            return node;
        }

        // 处理相等条件: x.Field == value
        if (node.NodeType == ExpressionType.Equal)
        {
            var result = ExtractFieldValue(node);
            if (result.HasValue)
            {
                var (fieldName, value) = result.Value;
                if (fieldName != null && value != null)
                {
                    // 检查字段是否在 OR 条件中出现过
                    if (!_analysis.FieldsInOrConditions.Contains(fieldName))
                    {
                        // 记录固定值（常量或成员访问）
                        _analysis.FixedValues[fieldName] = value;
                    }
                }
            }
            return node;
        }

        // 遇到 OR 运算符，只清空 OR 表达式中涉及字段的固定值
        if (node.NodeType == ExpressionType.OrElse)
        {
            // 收集 OR 表达式中涉及的所有字段名
            var fieldsInOr = CollectFieldNamesInExpression(node);
            foreach (var field in fieldsInOr)
            {
                _analysis.FixedValues.Remove(field);
                _analysis.FieldsInOrConditions.Add(field);  // 标记为出现在 OR 中
            }
            return node;
        }

        // 其他运算符（>, <, >=, <=, != 等）不影响固定值的收集
        return node;
    }

    /// <summary>
    /// 收集表达式中涉及的所有字段名
    /// </summary>
    private static HashSet<string> CollectFieldNamesInExpression(Expression expr)
    {
        var fields = new HashSet<string>();
        var collector = new FieldNameCollector(fields);
        collector.Visit(expr);
        return fields;
    }

    /// <summary>
    /// 提取字段名和值
    /// </summary>
    private static (string? fieldName, object? value)? ExtractFieldValue(BinaryExpression equalExpr)
    {
        Expression? fieldExpr = null;
        Expression? valueExpr = null;

        // 检查左侧是否为字段
        if (equalExpr.Left is MemberExpression leftMember && leftMember.Expression?.NodeType == ExpressionType.Parameter)
        {
            fieldExpr = equalExpr.Left;
            valueExpr = equalExpr.Right;
        }
        // 检查右侧是否为字段
        else if (equalExpr.Right is MemberExpression rightMember && rightMember.Expression?.NodeType == ExpressionType.Parameter)
        {
            fieldExpr = equalExpr.Right;
            valueExpr = equalExpr.Left;
        }
        else
        {
            return null;
        }

        // 获取字段名
        var fieldName = ((MemberExpression)fieldExpr).Member.Name;

        // 获取值（只支持常量和成员访问）
        var value = GetValue(valueExpr);

        return (fieldName, value);
    }

    /// <summary>
    /// 获取表达式的值
    /// </summary>
    private static object? GetValue(Expression expr)
    {
        if (expr is ConstantExpression constant)
        {
            return constant.Value;
        }

        if (expr is MemberExpression member)
        {
            // 编译并获取成员值
            try
            {
                var objectMember = Expression.Convert(member, typeof(object));
                var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                var getter = getterLambda.Compile();
                return getter();
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
