namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	[Serializable]
	public class TransportTypeInUseException : Exception
	{
		protected TransportTypeInUseException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public TransportTypeInUseException()
		{
		}

		public TransportTypeInUseException(string message) : base(message)
		{
		}

		public TransportTypeInUseException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public TransportTypeInUseException(string message, IReadOnlyCollection<TransportType> transportTypes, IReadOnlyCollection<Level> referencingLevels, IReadOnlyCollection<Endpoint> referencingEndpoints)
			: base(message)
		{
			TransportTypes = transportTypes ?? throw new ArgumentNullException(nameof(transportTypes));
			ReferencingLevels = referencingLevels ?? throw new ArgumentNullException(nameof(referencingLevels));
			ReferencingEndpoints = referencingEndpoints ?? throw new ArgumentNullException(nameof(referencingEndpoints));
		}

		public IReadOnlyCollection<TransportType> TransportTypes { get; }

		public IReadOnlyCollection<Level> ReferencingLevels { get; }

		public IReadOnlyCollection<Endpoint> ReferencingEndpoints { get; }
	}
}
