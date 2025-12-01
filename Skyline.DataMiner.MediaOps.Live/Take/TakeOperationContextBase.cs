namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;

	internal class TakeOperationContextBase
	{
		private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

		public TakeOperationContextBase(Endpoint destination)
		{
			Destination = destination ?? throw new ArgumentNullException(nameof(destination));
		}

		public Endpoint Destination { get; }

		public IDmsElement DestinationElement { get; set; }

		public MediationElement MediationElement { get; set; }

		public string ConnectionHandlerScript { get; set; }

		public bool IsSuccessful { get; set; }

		public Task CompletionTask => _taskCompletionSource.Task;

		public void SetCompleted()
		{
			_taskCompletionSource.TrySetResult(true);
		}
	}
}
