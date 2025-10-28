namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Exception thrown when one or more connection requests fail.
	/// </summary>
	public class ConnectFailedException : Exception
	{
		/// <summary>
		/// Gets the collection of connection requests that failed.
		/// </summary>
		public ICollection<ConnectionRequest> FailedRequests { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectFailedException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="failedRequests">The collection of connection requests that failed.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="failedRequests"/> is null.</exception>
		public ConnectFailedException(string message, ICollection<ConnectionRequest> failedRequests)
			: base(message)
		{
			FailedRequests = failedRequests ?? throw new ArgumentNullException(nameof(failedRequests));
		}
	}
}