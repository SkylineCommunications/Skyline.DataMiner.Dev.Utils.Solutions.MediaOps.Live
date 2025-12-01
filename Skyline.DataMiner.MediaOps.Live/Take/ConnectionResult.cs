namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Threading.Tasks;

	public abstract class ConnectionResult
	{
		public ConnectionResult(ConnectionRequest request)
		{
			ConnectionRequest = request ?? throw new ArgumentNullException(nameof(request));
		}

		public ConnectionRequest ConnectionRequest { get; }

		/// <summary>
		/// Gets a value indicating whether the connection operation was successful.
		/// </summary>
		public bool IsSuccessful { get; internal set; }

		/// <summary>
		/// Gets the task that represents the completion of the connection operation.
		/// </summary>
		public Task CompletionTask { get; internal set; }
	}

	public class EndpointConnectionResult : ConnectionResult
	{
		public EndpointConnectionResult(EndpointConnectionRequest request)
			: base(request)
		{
			EndpointConnectionRequest = request ?? throw new ArgumentNullException(nameof(request));
		}

		public EndpointConnectionRequest EndpointConnectionRequest { get; }
	}

	public class VsgConnectionResult : ConnectionResult
	{
		public VsgConnectionResult(VsgConnectionRequest request)
			: base(request)
		{
			VsgConnectionRequest = request ?? throw new ArgumentNullException(nameof(request));
		}

		public VsgConnectionRequest VsgConnectionRequest { get; }
	}
}
