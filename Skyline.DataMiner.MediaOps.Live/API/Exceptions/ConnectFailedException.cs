namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;

	public class ConnectFailedException : Exception
	{
		public ConnectFailedException()
		{
		}

		public ConnectFailedException(string message) : base(message)
		{
		}

		public ConnectFailedException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public ConnectFailedException(string message, ICollection<ConnectionRequest> failedRequests)
			: base(message)
		{
			FailedRequests = failedRequests ?? throw new ArgumentNullException(nameof(failedRequests));
		}

		public ICollection<ConnectionRequest> FailedRequests { get; }
	}
}