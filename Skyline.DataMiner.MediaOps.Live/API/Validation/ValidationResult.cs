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

		public void AddError(string message, object instance, string propertyName)
		{
			_errors.Add(new ValidationError(message, instance, propertyName));
		}

		public void AddError<T>(string message, T instance, Expression<Func<T, object>> expression)
		{
			var propertyName = GetPropertyName(expression);

			AddError(message, instance, propertyName);
		}

		public void Merge(ValidationResult result)
		{
			if (result is null)
			{
				throw new ArgumentNullException(nameof(result));
			}

			_errors.AddRange(result.Errors);
		}

		public ValidationResult ForInstance(object instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			var errors = Errors.Where(x => EqualityComparer<object>.Default.Equals(x.Instance, instance));

			return new ValidationResult(errors);
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

		public ValidationResult ForProperty(object instance, string propertyName)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			if (propertyName == null)
			{
				throw new ArgumentNullException(nameof(propertyName));
			}

			var errors = Errors.Where(x =>
				EqualityComparer<object>.Default.Equals(x.Instance, instance) &&
				String.Equals(propertyName, x.PropertyName));

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

		public ValidationResult ForProperty<T>(T instance, Expression<Func<T, object>> expression)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			if (expression == null)
			{
				throw new ArgumentNullException(nameof(expression));
			}

			var propertyName = GetPropertyName(expression);

			return ForProperty(instance, propertyName);
		}

		public void ThrowIfInvalid()
		{
			if (IsValid)
			{
				return;
			}

			var errorMessage = $"Validation failed:{Environment.NewLine}" +
				$"{String.Join(Environment.NewLine, Errors.Select(e => $"- {e.Message}"))}";

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
