namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;

	[Serializable]
	public class DisconnectFailedException : Exception
	{
		protected DisconnectFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public DisconnectFailedException()
		{
		}

		public DisconnectFailedException(string message) : base(message)
		{
		}

		public DisconnectFailedException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public DisconnectFailedException(string message, ICollection<DisconnectRequest> failedRequests)
			: base(message)
		{
			FailedRequests = failedRequests ?? throw new ArgumentNullException(nameof(failedRequests));
		}

		public ICollection<DisconnectRequest> FailedRequests { get; }
	}
}