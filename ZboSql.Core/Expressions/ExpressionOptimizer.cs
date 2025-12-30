using System.Data.Common;
using System.Linq.Expressions;
using ZboSql.Core.Infrastructure;

namespace ZboSql.Core.Expressions;

/// <summary>
/// 表达式优化结果
/// </summary>
public sealed class OptimizedExpression
{
    public string Sql { get; set; } = string.Empty;
    public List<DbParameter> Parameters { get; set; } = new();
    public bool IsOptimized { get; set; }
}

/// <summary>
/// SQL 表达式优化器
/// </summary>
public sealed class ExpressionOptimizer
{
    /// <summary>
    /// 尝试优化 OR 条件为 IN 查询
    /// </summary>
    public static OptimizedExpression? TryOptimizeOrIn(BinaryExpression node, IDbProvider provider, Type? entityType = null)
    {
        // 检查是否为 OR 表达式
        if (node.NodeType != ExpressionType.OrElse)
            return null;

        // 收集所有相等条件: field == value
        var conditions = new List<(Expression field, Expression value)>();
        CollectOrConditions(node, conditions);

        if (conditions.Count < 2)
            return null;

        // 检查是否都是对同一字段的比较
        var firstField = conditions[0].field;
        if (!conditions.All(c => IsSameField(c.field, firstField)))
            return null;

        // 可以优化为 IN 查询
        var sqlBuilder = new System.Text.StringBuilder();
        var parameters = new List<DbParameter>();

        // 获取字段名（需要实体类型信息来获取正确的列映射）
        var fieldName = GetColumnName(firstField, entityType);
        sqlBuilder.Append(provider.QuoteIdentifier(fieldName));
        sqlBuilder.Append(" IN (");

        // 添加参数
        for (int i = 0; i < conditions.Count; i++)
        {
            if (i > 0)
                sqlBuilder.Append(", ");

            var paramName = $"p{i}";
            var connection = provider.GetConnection();
            var parameter = connection.CreateCommand().CreateParameter();
            parameter.ParameterName = provider.FormatParameterName(paramName);

            // 获取值
            var value = GetExpressionValue(conditions[i].value);
            parameter.Value = value ?? DBNull.Value;
            parameters.Add(parameter);

            sqlBuilder.Append(parameter.ParameterName);
        }

        sqlBuilder.Append(')');

        return new OptimizedExpression
        {
            Sql = sqlBuilder.ToString(),
            Parameters = parameters,
            IsOptimized = true
        };
    }

    /// <summary>
    /// 收集 OR 条件
    /// </summary>
    private static void CollectOrConditions(Expression node, List<(Expression field, Expression value)> conditions)
    {
        if (node is BinaryExpression binary)
        {
            if (binary.NodeType == ExpressionType.OrElse)
            {
                // 递归收集左侧和右侧
                CollectOrConditions(binary.Left, conditions);
                CollectOrConditions(binary.Right, conditions);
            }
            else if (binary.NodeType == ExpressionType.Equal && IsMemberComparison(binary))
            {
                // field == value
                conditions.Add((binary.Left, binary.Right));
            }
        }
    }

    /// <summary>
    /// 检查是否为成员比较表达式
    /// </summary>
    private static bool IsMemberComparison(BinaryExpression binary)
    {
        // 检查是否为 x.Field == value 或 value == x.Field
        bool leftIsMember = IsFieldMember(binary.Left);
        bool rightIsMember = IsFieldMember(binary.Right);
        bool leftOrRightIsMember = leftIsMember || rightIsMember;

        // 检查另一边是否为常量值
        bool otherIsConstant = (leftIsMember && IsConstantValue(binary.Right)) ||
                               (rightIsMember && IsConstantValue(binary.Left));

        return leftOrRightIsMember && otherIsConstant;
    }

    /// <summary>
    /// 检查是否为字段成员表达式
    /// </summary>
    private static bool IsFieldMember(Expression expr)
    {
        if (expr is MemberExpression member && member.Expression?.NodeType == ExpressionType.Parameter)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检查是否为常量值表达式
    /// </summary>
    private static bool IsConstantValue(Expression expr)
    {
        return expr is ConstantExpression ||
               (expr is MemberExpression member && member.Expression?.NodeType != ExpressionType.Parameter);
    }

    /// <summary>
    /// 检查两个字段表达式是否相同
    /// </summary>
    private static bool IsSameField(Expression field1, Expression field2)
    {
        if (field1 is MemberExpression member1 && field2 is MemberExpression member2)
        {
            return member1.Member.Name == member2.Member.Name;
        }
        return false;
    }

    /// <summary>
    /// 获取列名
    /// </summary>
    private static string GetColumnName(Expression fieldExpr, Type? entityType)
    {
        if (fieldExpr is MemberExpression member)
        {
            var propertyName = member.Member.Name;

            // 如果有实体类型信息，尝试从EntityInfo获取正确的列名
            if (entityType != null)
            {
                var entityInfo = EntityInfo.Get(entityType);
                var column = entityInfo.Columns.FirstOrDefault(c => c.PropertyName == propertyName);
                if (column != null)
                {
                    return column.ColumnName;
                }
            }

            // 降级：使用属性名转小写
            return propertyName.ToLowerInvariant();
        }
        return "unknown";
    }

    /// <summary>
    /// 获取表达式值
    /// </summary>
    private static object? GetExpressionValue(Expression expr)
    {
        switch (expr)
        {
            case ConstantExpression constant:
                return constant.Value;
            case MemberExpression member:
                // 编译 lambda 获取成员值
                var objectMember = Expression.Convert(member, typeof(object));
                var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                var getter = getterLambda.Compile();
                return getter();
            default:
                var lambda = Expression.Lambda<Func<object>>(Expression.Convert(expr, typeof(object)));
                return lambda.Compile()();
        }
    }
}
