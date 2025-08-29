namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class EndpointConnection : IEquatable<EndpointConnection>
	{
		public EndpointConnection(Endpoint endpoint, EndpointConnectionState state)
		{
			Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
			State = state;
		}

		public Endpoint Endpoint { get; }

		public EndpointConnectionState State { get; }

		public bool IsConnected => State is EndpointConnectionState.Connected or EndpointConnectionState.Disconnecting;

		public override bool Equals(object obj)
		{
			return Equals(obj as EndpointConnection);
		}

		public bool Equals(EndpointConnection other)
		{
			return other is not null &&
				   EqualityComparer<Endpoint>.Default.Equals(Endpoint, other.Endpoint) &&
				   State == other.State;
		}

		public override int GetHashCode()
		{
			return (Endpoint, State).GetHashCode();
		}

		public override string ToString()
		{
			return $"{Endpoint} - {State}";
		}

		public static bool operator ==(EndpointConnection left, EndpointConnection right)
		{
			return EqualityComparer<EndpointConnection>.Default.Equals(left, right);
		}

		public static bool operator !=(EndpointConnection left, EndpointConnection right)
		{
			return !(left == right);
		}
	}
}
