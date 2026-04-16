namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	[Serializable]
	public class EndpointInUseException : Exception
	{
		protected EndpointInUseException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public EndpointInUseException()
		{
		}

		public EndpointInUseException(string message) : base(message)
		{
		}

		public EndpointInUseException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public EndpointInUseException(string message, IReadOnlyCollection<Endpoint> endpoints, IReadOnlyCollection<VirtualSignalGroup> referencingVirtualSignalGroups)
			: base(message)
		{
			Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
			ReferencingVirtualSignalGroups = referencingVirtualSignalGroups ?? throw new ArgumentNullException(nameof(referencingVirtualSignalGroups));
		}

		public IReadOnlyCollection<Endpoint> Endpoints { get; }

		public IReadOnlyCollection<VirtualSignalGroup> ReferencingVirtualSignalGroups { get; }
	}
}
