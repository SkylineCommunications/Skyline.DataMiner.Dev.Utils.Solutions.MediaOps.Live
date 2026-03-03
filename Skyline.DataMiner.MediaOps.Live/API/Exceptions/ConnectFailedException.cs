namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;

	[Serializable]
	public class ConnectFailedException : Exception
	{
		protected ConnectFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

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