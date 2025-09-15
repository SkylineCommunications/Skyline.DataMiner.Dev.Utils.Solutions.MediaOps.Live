namespace Skyline.DataMiner.MediaOps.Live.API.Querying
{
	using System;
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Reflection;

	internal static class ExpressionTools
	{
		public static bool TryGetMember(Expression expression, out MemberInfo memberInfo, out string propertyPath)
		{
			if (expression is UnaryExpression unary)
			{
				return TryGetMember(unary.Operand, out memberInfo, out propertyPath);
			}

			if (expression is LambdaExpression lambda)
			{
				return TryGetMember(lambda.Body, out memberInfo, out propertyPath);
			}

			if (expression is MemberExpression memberExpression)
			{
				memberInfo = memberExpression.Member;
				propertyPath = GetPropertyPath(memberExpression);
				return true;
			}

			memberInfo = null;
			propertyPath = null;
			return false;
		}

		public static bool TryGetValue(Expression expression, out object value)
		{
			switch (expression)
			{
				case UnaryExpression unary:
					return TryGetValue(unary.Operand, out value);

				case LambdaExpression lambda:
					return TryGetValue(lambda.Body, out value);

				case ConstantExpression constant:
					value = constant.Value;
					return true;

				case MemberExpression member when member.Member is FieldInfo fieldInfo && fieldInfo.IsStatic:
					value = fieldInfo.GetValue(null);
					return true;

				case MemberExpression member when member.Member is PropertyInfo propertyInfo && propertyInfo.GetGetMethod().IsStatic:
					value = propertyInfo.GetValue(null);
					return true;
			}

			try
			{
				var compiledLambda = Expression.Lambda(expression).Compile();
				value = compiledLambda.DynamicInvoke();
				return true;
			}
			catch (Exception)
			{
				value = null;
				return false;
			}
		}

		private static string GetPropertyPath(Expression expr)
		{
			var memberNames = new Stack<string>();

			while (expr is MemberExpression memberExpr)
			{
				memberNames.Push(memberExpr.Member.Name);
				expr = memberExpr.Expression;
			}

			return String.Join(".", memberNames);
		}
	}
}
