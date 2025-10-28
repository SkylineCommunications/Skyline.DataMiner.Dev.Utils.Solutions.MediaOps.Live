namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	/// <summary>
	/// The exception that is thrown when a disconnect operation fails.
	/// </summary>
	[Serializable]
	public class DisconnectFailedException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DisconnectFailedException"/> class.
		/// </summary>
		public DisconnectFailedException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisconnectFailedException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public DisconnectFailedException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisconnectFailedException"/> class with a specified error message
		/// and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public DisconnectFailedException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisconnectFailedException"/> class with a specified error message
		/// and a collection of failed requests.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="failedRequests">The collection of disconnect requests that failed.</param>
		/// <exception cref="ArgumentNullException"><paramref name="failedRequests"/> is <see langword="null"/>.</exception>
		public DisconnectFailedException(string message, ICollection<DisconnectRequest> failedRequests)
			: base(message)
		{
			FailedRequests = failedRequests ?? throw new ArgumentNullException(nameof(failedRequests));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisconnectFailedException"/> class with serialized data.
		/// </summary>
		/// <param name="info">The object that holds the serialized object data.</param>
		/// <param name="context">The contextual information about the source or destination.</param>
		protected DisconnectFailedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		/// <summary>
		/// Gets the collection of disconnect requests that failed.
		/// </summary>
		public ICollection<DisconnectRequest> FailedRequests { get; }
	}
}