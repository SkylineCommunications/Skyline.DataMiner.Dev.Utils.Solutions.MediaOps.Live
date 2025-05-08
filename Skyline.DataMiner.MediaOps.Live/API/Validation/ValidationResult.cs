namespace Skyline.DataMiner.MediaOps.Live.API.Validation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;

	public class ValidationResult
	{
		private readonly List<ValidationError> _errors = new List<ValidationError>();

		public ValidationResult()
		{
		}

		public ValidationResult(IEnumerable<ValidationError> errors)
		{
			if (errors == null)
			{
				throw new ArgumentNullException(nameof(errors));
			}

			_errors.AddRange(errors);
		}

		public IReadOnlyCollection<ValidationError> Errors => _errors;

		public bool IsValid => Errors.Count == 0;

		public void AddError(string message)
		{
			_errors.Add(new ValidationError(message));
		}

		public void AddError(string message, string propertyName)
		{
			_errors.Add(new ValidationError(message, propertyName));
		}

		public void AddError<T>(string message, Expression<Func<T, object>> expression)
		{
			var propertyName = GetPropertyName(expression);

			AddError(message, propertyName);
		}

		public ValidationResult ForProperty(string propertyName)
		{
			if (propertyName == null)
			{
				throw new ArgumentNullException(nameof(propertyName));
			}

			var errors = Errors.Where(x => String.Equals(propertyName, x.PropertyName));

			return new ValidationResult(errors);
		}

		public ValidationResult ForProperty<T>(Expression<Func<T, object>> expression)
		{
			if (expression == null)
			{
				throw new ArgumentNullException(nameof(expression));
			}

			var propertyName = GetPropertyName(expression);

			return ForProperty(propertyName);
		}

		public void ThrowIfInvalid()
		{
			if (IsValid)
			{
				return;
			}

			var errorMessages = Errors.Select(e =>
				String.IsNullOrEmpty(e.PropertyName)
					? $"- {e.Message}"
					: $"- {e.PropertyName}: {e.Message}");

			var errorMessage = $"Validation failed:{Environment.NewLine}{String.Join(Environment.NewLine, errorMessages)}";

			throw new Exception(errorMessage);
		}

		private static string GetPropertyName<T>(Expression<Func<T, object>> expression)
		{
			if (expression.Body is MemberExpression member)
			{
				return member.Member.Name;
			}

			if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression memberOperand)
			{
				return memberOperand.Member.Name;
			}

			throw new ArgumentException("Invalid property expression");
		}
	}
}
