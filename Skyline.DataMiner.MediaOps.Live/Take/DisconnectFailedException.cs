namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Exception thrown when one or more disconnect requests fail.
	/// </summary>
	public class DisconnectFailedException : Exception
	{
		/// <summary>
		/// Gets the collection of disconnect requests that failed.
		/// </summary>
		public ICollection<DisconnectRequest> FailedRequests { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DisconnectFailedException"/> class.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="failedRequests">The collection of disconnect requests that failed.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="failedRequests"/> is null.</exception>
		public DisconnectFailedException(string message, ICollection<DisconnectRequest> failedRequests)
			: base(message)
		{
			FailedRequests = failedRequests ?? throw new ArgumentNullException(nameof(failedRequests));
		}
	}
}