namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;

	public class ConnectFailedException : Exception
	{
		public ICollection<ConnectionRequest> FailedRequests { get; }

		public ConnectFailedException(string message, ICollection<ConnectionRequest> failedRequests)
			: base(message)
		{
			FailedRequests = failedRequests ?? throw new ArgumentNullException(nameof(failedRequests));
		}
	}
}