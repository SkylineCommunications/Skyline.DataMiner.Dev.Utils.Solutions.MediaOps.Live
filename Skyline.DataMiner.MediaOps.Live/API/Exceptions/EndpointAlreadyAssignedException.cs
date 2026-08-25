namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Thrown when an endpoint is assigned to more than one level of the same destination virtual signal group.
	/// Assigning the same endpoint to multiple levels is allowed for source virtual signal groups.
	/// </summary>
	[Serializable]
	public class EndpointAlreadyAssignedException : Exception
	{
		protected EndpointAlreadyAssignedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public EndpointAlreadyAssignedException()
		{
		}

		public EndpointAlreadyAssignedException(string message) : base(message)
		{
		}

		public EndpointAlreadyAssignedException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public EndpointAlreadyAssignedException(string message, ApiObjectReference<Endpoint> endpoint, ApiObjectReference<Level> assignedLevel)
			: base(message)
		{
			Endpoint = endpoint;
			AssignedLevel = assignedLevel;
		}

		/// <summary>
		/// Gets the endpoint that is already assigned.
		/// </summary>
		public ApiObjectReference<Endpoint> Endpoint { get; }

		/// <summary>
		/// Gets the level the endpoint is already assigned to.
		/// </summary>
		public ApiObjectReference<Level> AssignedLevel { get; }
	}
}
