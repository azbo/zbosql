using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ZboSql.Core.Infrastructure;

namespace ZboSql.Core.Expressions;

/// <summary>
/// 表达式解析结果
/// </summary>
public sealed class ParseResult
{
    public string Sql { get; set; } = string.Empty;
    public List<DbParameter> Parameters { get; set; } = new();
}

/// <summary>
/// 参数索引引用（用于在解析器中共享状态）
/// </summary>
internal sealed class ParamIndexRef
{
    public int Value { get; set; }
}

/// <summary>
/// SQL 表达式解析器
/// </summary>
public sealed class ExpressionParser
{
    private readonly IDbProvider _provider;
    private readonly StringBuilder _sql;
    private readonly List<DbParameter> _parameters;
    private readonly ParamIndexRef _paramIndexRef;

    public ExpressionParser(IDbProvider provider)
    {
        _provider = provider;
        _sql = new StringBuilder();
        _parameters = new List<DbParameter>();
        _paramIndexRef = new ParamIndexRef();
    }

    /// <summary>
    /// 解析 WHERE 表达式
    /// </summary>
    public ParseResult ParseWhere<T>(Expression<Func<T, bool>>? expression)
    {
        if (expression == null)
            return new ParseResult { Sql = "", Parameters = new List<DbParameter>() };

        _sql.Clear();
        _parameters.Clear();
        _paramIndexRef.Value = 0;

        var visitor = new SqlVisitor(_provider, _sql, _parameters, _paramIndexRef);
        visitor.Visit(expression);

        return new ParseResult
        {
            Sql = _sql.ToString(),
            Parameters = new List<DbParameter>(_parameters)
        };
    }

    /// <summary>
    /// 解析 WHERE 表达式（使用 LambdaExpression 和指定类型）
    /// </summary>
    public ParseResult ParseWhere(Type sourceType, LambdaExpression? expression)
    {
        if (expression == null)
            return new ParseResult { Sql = "", Parameters = new List<DbParameter>() };

        _sql.Clear();
        _parameters.Clear();
        _paramIndexRef.Value = 0;

        var visitor = new SqlVisitor(_provider, _sql, _parameters, _paramIndexRef, sourceType);
        visitor.Visit(expression);

        return new ParseResult
        {
            Sql = _sql.ToString(),
            Parameters = new List<DbParameter>(_parameters)
        };
    }
}

/// <summary>
/// SQL 表达式访问器
/// </summary>
internal sealed class SqlVisitor : ExpressionVisitor
{
    private readonly IDbProvider _provider;
    private readonly StringBuilder _sql;
    private readonly List<DbParameter> _parameters;
    private readonly ParamIndexRef _paramIndexRef;
    private readonly Type? _sourceType;

    public SqlVisitor(
        IDbProvider provider,
        StringBuilder sql,
        List<DbParameter> parameters,
        ParamIndexRef paramIndexRef)
        : this(provider, sql, parameters, paramIndexRef, null)
    {
    }

    public SqlVisitor(
        IDbProvider provider,
        StringBuilder sql,
        List<DbParameter> parameters,
        ParamIndexRef paramIndexRef,
        Type? sourceType)
    {
        _provider = provider;
        _sql = sql;
        _parameters = parameters;
        _paramIndexRef = paramIndexRef;
        _sourceType = sourceType;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        return Visit(node.Body);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // 尝试优化 OR 为 IN 查询
        if (node.NodeType == ExpressionType.OrElse)
        {
            var optimized = ExpressionOptimizer.TryOptimizeOrIn(node, _provider, _sourceType);
            if (optimized != null && optimized.IsOptimized)
            {
                // 使用优化后的 IN 查询
                _sql.Append('(');
                _sql.Append(optimized.Sql);
                _sql.Append(')');

                // 添加参数
                foreach (var param in optimized.Parameters)
                {
                    _parameters.Add(param);
                }
                _paramIndexRef.Value += optimized.Parameters.Count;

                return node;
            }
        }

        // 常规处理
        _sql.Append('(');
        Visit(node.Left);

        _sql.Append(node.NodeType switch
        {
            ExpressionType.Equal => " = ",
            ExpressionType.NotEqual => " != ",
            ExpressionType.GreaterThan => " > ",
            ExpressionType.GreaterThanOrEqual => " >= ",
            ExpressionType.LessThan => " < ",
            ExpressionType.LessThanOrEqual => " <= ",
            ExpressionType.AndAlso => " AND ",
            ExpressionType.OrElse => " OR ",
            ExpressionType.Add => " + ",
            ExpressionType.Subtract => " - ",
            ExpressionType.Multiply => " * ",
            ExpressionType.Divide => " / ",
            _ => throw new NotSupportedException($"Binary operator '{node.NodeType}' is not supported")
        });

        Visit(node.Right);
        _sql.Append(')');

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // 处理属性访问：x.Name
        if (node.Expression?.NodeType == ExpressionType.Parameter)
        {
            var entityType = _sourceType ?? node.Expression.Type;
            var entityInfo = EntityInfo.Get(entityType);
            var column = entityInfo.Columns.FirstOrDefault(c => c.PropertyName == node.Member.Name);
            if (column != null)
            {
                _sql.Append(_provider.QuoteIdentifier(column.ColumnName));
            }
            else
            {
                _sql.Append(_provider.QuoteIdentifier(node.Member.Name.ToLowerInvariant()));
            }
            return node;
        }

        // 处理常量成员访问
        var value = GetMemberValue(node);
        AddParameter(value);
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        AddParameter(node.Value);
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // 处理字符串方法
        if (node.Method.DeclaringType == typeof(string))
        {
            return VisitStringMethod(node);
        }

        return base.VisitMethodCall(node);
    }

    private Expression VisitStringMethod(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case "Contains":
                // x.Name.Contains("abc") -> "name" LIKE @p0
                Visit(node.Object);
                _sql.Append(" LIKE ");
                var containsValue = GetExpressionValue(node.Arguments[0]);
                AddParameter($"%{containsValue}%");
                break;

            case "StartsWith":
                // x.Name.StartsWith("abc") -> "name" LIKE @p0
                Visit(node.Object);
                _sql.Append(" LIKE ");
                var startsWithValue = GetExpressionValue(node.Arguments[0]);
                AddParameter($"{startsWithValue}%");
                break;

            case "EndsWith":
                // x.Name.EndsWith("abc") -> "name" LIKE @p0
                Visit(node.Object);
                _sql.Append(" LIKE ");
                var endsWithValue = GetExpressionValue(node.Arguments[0]);
                AddParameter($"%{endsWithValue}");
                break;

            default:
                throw new NotSupportedException($"String method '{node.Method.Name}' is not supported");
        }

        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Not:
                _sql.Append("NOT ");
                Visit(node.Operand);
                break;

            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                Visit(node.Operand);
                break;

            default:
                throw new NotSupportedException($"Unary operator '{node.NodeType}' is not supported");
        }

        return node;
    }

    private void AddParameter(object? value)
    {
        var paramName = $"p{_paramIndexRef.Value++}";
        var connection = _provider.GetConnection();
        var parameter = connection.CreateCommand().CreateParameter();
        parameter.ParameterName = _provider.FormatParameterName(paramName);
        parameter.Value = value ?? DBNull.Value;
        _parameters.Add(parameter);

        _sql.Append(parameter.ParameterName);
    }

    private static object? GetMemberValue(MemberExpression memberExpression)
    {
        var objectMember = Expression.Convert(memberExpression, typeof(object));
        var getterLambda = Expression.Lambda<Func<object>>(objectMember);
        var getter = getterLambda.Compile();
        return getter();
    }

    private static object? GetExpressionValue(Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression constant:
                return constant.Value;
            case MemberExpression member:
                return GetMemberValue(member);
            default:
                var lambda = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object)));
                return lambda.Compile()();
        }
    }
}
