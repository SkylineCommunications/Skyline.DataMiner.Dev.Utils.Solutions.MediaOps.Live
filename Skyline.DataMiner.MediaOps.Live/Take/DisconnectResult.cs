namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System.Threading.Tasks;

	public abstract class DisconnectResult<T> : TakeResult<T> where T : DisconnectRequest
	{
		protected DisconnectResult(T request, bool isSuccessful, Task<bool> completionTask)
			: base(request, isSuccessful, completionTask)
		{
		}
	}

	public class EndpointDisconnectResult : DisconnectResult<EndpointDisconnectRequest>
	{
		public EndpointDisconnectResult(EndpointDisconnectRequest request, bool isSuccessful, Task<bool> completionTask)
			: base(request, isSuccessful, completionTask)
		{
		}
	}

	public class VsgDisconnectResult : DisconnectResult<VsgDisconnectRequest>
	{
		public VsgDisconnectResult(VsgDisconnectRequest request, bool isSuccessful, Task<bool> completionTask)
			: base(request, isSuccessful, completionTask)
		{
		}
	}
}
