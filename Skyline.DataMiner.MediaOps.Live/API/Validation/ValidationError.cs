namespace Skyline.DataMiner.MediaOps.Live.API.Validation
{
	using System;

	/// <summary>
	/// Represents a validation error with details about the error message, instance, and property.
	/// </summary>
	public class ValidationError
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ValidationError"/> class.
		/// </summary>
		/// <param name="message">The validation error message.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is null or whitespace.</exception>
		public ValidationError(string message)
		{
			if (String.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentException($"'{nameof(message)}' cannot be null or whitespace.", nameof(message));
			}

			Message = message;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ValidationError"/> class.
		/// </summary>
		/// <param name="message">The validation error message.</param>
		/// <param name="instance">The object instance that failed validation.</param>
		/// <param name="propertyName">The name of the property that failed validation.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="message"/> or <paramref name="propertyName"/> is null or whitespace.</exception>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> is null.</exception>
		public ValidationError(string message, object instance, string propertyName)
		{
			if (String.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentException($"'{nameof(message)}' cannot be null or whitespace.", nameof(message));
			}

			if (instance is null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			if (String.IsNullOrWhiteSpace(propertyName))
			{
				throw new ArgumentException($"'{nameof(propertyName)}' cannot be null or whitespace.", nameof(propertyName));
			}

			Message = message;
			Instance = instance;
			PropertyName = propertyName;
		}

		/// <summary>
		/// Gets the validation error message.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Gets the object instance that failed validation.
		/// </summary>
		public object Instance { get; }

		/// <summary>
		/// Gets the name of the property that failed validation.
		/// </summary>
		public string PropertyName { get; }
	}
}
