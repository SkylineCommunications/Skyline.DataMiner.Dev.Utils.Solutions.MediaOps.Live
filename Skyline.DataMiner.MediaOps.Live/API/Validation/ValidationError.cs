namespace Skyline.DataMiner.MediaOps.Live.API.Validation
{
	using System;

	public class ValidationError
	{
		public ValidationError(string message)
		{
			if (String.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentException($"'{nameof(message)}' cannot be null or whitespace.", nameof(message));
			}

			Message = message;
		}

		public ValidationError(string message, string propertyName)
		{
			if (String.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentException($"'{nameof(message)}' cannot be null or whitespace.", nameof(message));
			}

			if (String.IsNullOrWhiteSpace(propertyName))
			{
				throw new ArgumentException($"'{nameof(propertyName)}' cannot be null or whitespace.", nameof(propertyName));
			}

			Message = message;
			PropertyName = propertyName;
		}

		public string Message { get; }

		public string PropertyName { get; }
	}
}
