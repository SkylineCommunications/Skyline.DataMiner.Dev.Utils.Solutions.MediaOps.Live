namespace Skyline.DataMiner.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Take;

	public class DisconnectFailedException : Exception
	{
		public DisconnectFailedException()
		{
		}

		public DisconnectFailedException(string message) : base(message)
		{
		}

		public DisconnectFailedException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public DisconnectFailedException(string message, ICollection<VsgDisconnectRequest> failedRequests)
			: base(message)
		{
			FailedRequests = failedRequests ?? throw new ArgumentNullException(nameof(failedRequests));
		}

		public ICollection<VsgDisconnectRequest> FailedRequests { get; }
	}
}