using System;
using System.Linq.Expressions;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// 提供对 <see cref="T:System.Linq.Expressions.Expression" /> 类型的扩展方法，用于组合和操作表达式树。
/// </summary>
public static class ExpressionExtension
{
	/// <summary>
	/// 将两个表达式进行逻辑与运算，使用短路求值。
	/// </summary>
	/// <typeparam name="T">表达式的参数类型。</typeparam>
	/// <param name="leftExpression">第一个表达式，作为逻辑与运算的左操作数。</param>
	/// <param name="rightExpression">第二个表达式，作为逻辑与运算的右操作数。</param>
	/// <returns>一个新的表达式，表示两个输入表达式的逻辑与运算结果。</returns>
	/// <exception cref="T:System.ArgumentNullException">当 leftExpression 或 rightExpression 为 null 时抛出。</exception>
	public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> leftExpression, Expression<Func<T, bool>> rightExpression)
	{
		ArgumentNullException.ThrowIfNull(leftExpression, "leftExpression");
		ArgumentNullException.ThrowIfNull(rightExpression, "rightExpression");
		ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "And");
		ExpressionVisitorCustom expressionVisitorCustom = new ExpressionVisitorCustom(parameterExpression);
		Expression left = expressionVisitorCustom.Visit(leftExpression.Body);
		Expression right = expressionVisitorCustom.Visit(rightExpression.Body);
		return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), new ParameterExpression[1] { parameterExpression });
	}

	/// <summary>
	/// 根据条件将两个表达式进行逻辑与运算，使用短路求值。
	/// 当条件为false时，仅返回左表达式；当条件为true时，返回两个表达式的逻辑与运算结果。
	/// </summary>
	/// <typeparam name="T">表达式的参数类型。</typeparam>
	/// <param name="leftExpression">第一个表达式，作为逻辑与运算的左操作数。</param>
	/// <param name="condition">决定是否执行逻辑与运算的条件委托。</param>
	/// <param name="rightExpression">第二个表达式，作为逻辑与运算的右操作数。</param>
	/// <returns>当条件为true时返回两个表达式的逻辑与运算结果，否则返回左表达式。</returns>
	/// <exception cref="T:System.ArgumentNullException">当任何参数为null时抛出。</exception>
	public static Expression<Func<T, bool>> AndIf<T>(this Expression<Func<T, bool>> leftExpression, Func<bool> condition, Expression<Func<T, bool>> rightExpression)
	{
		ArgumentNullException.ThrowIfNull(leftExpression, "leftExpression");
		ArgumentNullException.ThrowIfNull(condition, "condition");
		ArgumentNullException.ThrowIfNull(rightExpression, "rightExpression");
		if (!condition())
		{
			return leftExpression;
		}
		ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "AndIf");
		ExpressionVisitorCustom expressionVisitorCustom = new ExpressionVisitorCustom(parameterExpression);
		Expression left = expressionVisitorCustom.Visit(leftExpression.Body);
		Expression right = expressionVisitorCustom.Visit(rightExpression.Body);
		return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), new ParameterExpression[1] { parameterExpression });
	}

	/// <summary>
	/// 将两个表达式进行逻辑或运算，使用短路求值。
	/// </summary>
	/// <typeparam name="T">表达式的参数类型。</typeparam>
	/// <param name="leftExpression">第一个表达式，作为逻辑或运算的左操作数。</param>
	/// <param name="rightExpression">第二个表达式，作为逻辑或运算的右操作数。</param>
	/// <returns>一个新的表达式，表示两个输入表达式的逻辑或运算结果。</returns>
	/// <exception cref="T:System.ArgumentNullException">当 leftExpression 或 rightExpression 为 null 时抛出。</exception>
	public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> leftExpression, Expression<Func<T, bool>> rightExpression)
	{
		ArgumentNullException.ThrowIfNull(leftExpression, "leftExpression");
		ArgumentNullException.ThrowIfNull(rightExpression, "rightExpression");
		ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "Or");
		ExpressionVisitorCustom expressionVisitorCustom = new ExpressionVisitorCustom(parameterExpression);
		Expression left = expressionVisitorCustom.Visit(leftExpression.Body);
		Expression right = expressionVisitorCustom.Visit(rightExpression.Body);
		return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left, right), new ParameterExpression[1] { parameterExpression });
	}

	/// <summary>
	/// 根据条件将两个表达式进行逻辑或运算，使用短路求值。
	/// 当条件为false时，仅返回左表达式；当条件为true时，返回两个表达式的逻辑或运算结果。
	/// </summary>
	/// <typeparam name="T">表达式的参数类型。</typeparam>
	/// <param name="leftExpression">第一个表达式，作为逻辑或运算的左操作数。</param>
	/// <param name="condition">决定是否执行逻辑或运算的条件委托。</param>
	/// <param name="rightExpression">第二个表达式，作为逻辑或运算的右操作数。</param>
	/// <returns>当条件为true时返回两个表达式的逻辑或运算结果，否则返回左表达式。</returns>
	/// <exception cref="T:System.ArgumentNullException">当任何参数为null时抛出。</exception>
	public static Expression<Func<T, bool>> OrIf<T>(this Expression<Func<T, bool>> leftExpression, Func<bool> condition, Expression<Func<T, bool>> rightExpression)
	{
		ArgumentNullException.ThrowIfNull(leftExpression, "leftExpression");
		ArgumentNullException.ThrowIfNull(condition, "condition");
		ArgumentNullException.ThrowIfNull(rightExpression, "rightExpression");
		if (!condition())
		{
			return leftExpression;
		}
		ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "OrIf");
		ExpressionVisitorCustom expressionVisitorCustom = new ExpressionVisitorCustom(parameterExpression);
		Expression left = expressionVisitorCustom.Visit(leftExpression.Body);
		Expression right = expressionVisitorCustom.Visit(rightExpression.Body);
		return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left, right), new ParameterExpression[1] { parameterExpression });
	}

	/// <summary>
	/// 对表达式进行逻辑非运算，对表达式的结果取反。
	/// </summary>
	/// <typeparam name="T">表达式的参数类型。</typeparam>
	/// <param name="expr">要进行逻辑非运算的表达式。</param>
	/// <returns>一个新的表达式，表示输入表达式的逻辑非运算结果。</returns>
	/// <exception cref="T:System.ArgumentNullException">当 expr 为 null 时抛出。</exception>
	/// <remarks>
	/// 如果输入表达式为 x =&gt; x &gt; 5，则输出表达式为 x =&gt; !(x &gt; 5)，等价于 x =&gt; x &lt;= 5。
	/// </remarks>
	public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expr)
	{
		ArgumentNullException.ThrowIfNull(expr, "expr");
		ParameterExpression parameterExpression = expr.Parameters[0];
		return Expression.Lambda<Func<T, bool>>(Expression.Not(expr.Body), new ParameterExpression[1] { parameterExpression });
	}
}
