namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;

	public class DisconnectFailedException : Exception
	{
		public ICollection<DisconnectRequest> FailedRequests { get; }

		public DisconnectFailedException(string message, ICollection<DisconnectRequest> failedRequests)
			: base(message)
		{
			FailedRequests = failedRequests ?? throw new ArgumentNullException(nameof(failedRequests));
		}
	}
}