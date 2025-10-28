namespace Skyline.DataMiner.MediaOps.Live.API.Validation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;

	/// <summary>
	/// Represents the result of a validation operation, containing validation errors if any.
	/// </summary>
	public class ValidationResult
	{
		private readonly List<ValidationError> _errors = new List<ValidationError>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ValidationResult"/> class.
		/// </summary>
		public ValidationResult()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ValidationResult"/> class with the specified errors.
		/// </summary>
		/// <param name="errors">The collection of validation errors.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
		public ValidationResult(IEnumerable<ValidationError> errors)
		{
			if (errors == null)
			{
				throw new ArgumentNullException(nameof(errors));
			}

			_errors.AddRange(errors);
		}

		/// <summary>
		/// Gets the collection of validation errors.
		/// </summary>
		public IReadOnlyCollection<ValidationError> Errors => _errors;

		/// <summary>
		/// Gets a value indicating whether the validation was successful (no errors).
		/// </summary>
		public bool IsValid => Errors.Count == 0;

		/// <summary>
		/// Adds a validation error with the specified message.
		/// </summary>
		/// <param name="message">The validation error message.</param>
		public void AddError(string message)
		{
			_errors.Add(new ValidationError(message));
		}

		/// <summary>
		/// Adds a validation error for a specific property.
		/// </summary>
		/// <param name="message">The validation error message.</param>
		/// <param name="instance">The object instance that failed validation.</param>
		/// <param name="propertyName">The name of the property that failed validation.</param>
		public void AddError(string message, object instance, string propertyName)
		{
			_errors.Add(new ValidationError(message, instance, propertyName));
		}

		/// <summary>
		/// Adds a validation error for a specific property using a lambda expression.
		/// </summary>
		/// <typeparam name="T">The type of the object instance.</typeparam>
		/// <param name="message">The validation error message.</param>
		/// <param name="instance">The object instance that failed validation.</param>
		/// <param name="expression">An expression identifying the property.</param>
		public void AddError<T>(string message, T instance, Expression<Func<T, object>> expression)
		{
			var propertyName = GetPropertyName(expression);

			AddError(message, instance, propertyName);
		}

		/// <summary>
		/// Merges another validation result into this one.
		/// </summary>
		/// <param name="result">The validation result to merge.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
		public void Merge(ValidationResult result)
		{
			if (result is null)
			{
				throw new ArgumentNullException(nameof(result));
			}

			_errors.AddRange(result.Errors);
		}

		/// <summary>
		/// Gets a validation result containing only errors for the specified instance.
		/// </summary>
		/// <param name="instance">The object instance to filter by.</param>
		/// <returns>A new <see cref="ValidationResult"/> containing only errors for the specified instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> is null.</exception>
		public ValidationResult ForInstance(object instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			var errors = Errors.Where(x => EqualityComparer<object>.Default.Equals(x.Instance, instance));

			return new ValidationResult(errors);
		}

		/// <summary>
		/// Gets a validation result containing only errors for the specified property name.
		/// </summary>
		/// <param name="propertyName">The property name to filter by.</param>
		/// <returns>A new <see cref="ValidationResult"/> containing only errors for the specified property.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
		public ValidationResult ForProperty(string propertyName)
		{
			if (propertyName == null)
			{
				throw new ArgumentNullException(nameof(propertyName));
			}

			var errors = Errors.Where(x => String.Equals(propertyName, x.PropertyName));

			return new ValidationResult(errors);
		}

		/// <summary>
		/// Gets a validation result containing only errors for the specified instance and property.
		/// </summary>
		/// <param name="instance">The object instance to filter by.</param>
		/// <param name="propertyName">The property name to filter by.</param>
		/// <returns>A new <see cref="ValidationResult"/> containing only errors matching the criteria.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> or <paramref name="propertyName"/> is null.</exception>
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

		/// <summary>
		/// Gets a validation result containing only errors for the specified property using a lambda expression.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="expression">An expression identifying the property.</param>
		/// <returns>A new <see cref="ValidationResult"/> containing only errors for the specified property.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
		public ValidationResult ForProperty<T>(Expression<Func<T, object>> expression)
		{
			if (expression == null)
			{
				throw new ArgumentNullException(nameof(expression));
			}

			var propertyName = GetPropertyName(expression);

			return ForProperty(propertyName);
		}

		/// <summary>
		/// Gets a validation result containing only errors for the specified instance and property using a lambda expression.
		/// </summary>
		/// <typeparam name="T">The type of the object instance.</typeparam>
		/// <param name="instance">The object instance to filter by.</param>
		/// <param name="expression">An expression identifying the property.</param>
		/// <returns>A new <see cref="ValidationResult"/> containing only errors matching the criteria.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> or <paramref name="expression"/> is null.</exception>
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

		/// <summary>
		/// Throws an exception if the validation result contains any errors.
		/// </summary>
		/// <exception cref="Exception">Thrown when the validation result contains errors.</exception>
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
