namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System.Threading.Tasks;

	public abstract class ConnectionResult<T> : TakeResult<T> where T : ConnectionRequest
	{
		protected ConnectionResult(T request, bool isSuccessful, Task<bool> completionTask)
			: base(request, isSuccessful, completionTask)
		{
		}
	}

	public class EndpointConnectionResult : ConnectionResult<EndpointConnectionRequest>
	{
		public EndpointConnectionResult(EndpointConnectionRequest request, bool isSuccessful, Task<bool> completionTask)
			: base(request, isSuccessful, completionTask)
		{
		}
	}

	public class VsgConnectionResult : ConnectionResult<VsgConnectionRequest>
	{
		public VsgConnectionResult(VsgConnectionRequest request, bool isSuccessful, Task<bool> completionTask)
			: base(request, isSuccessful, completionTask)
		{
		}
	}
}
