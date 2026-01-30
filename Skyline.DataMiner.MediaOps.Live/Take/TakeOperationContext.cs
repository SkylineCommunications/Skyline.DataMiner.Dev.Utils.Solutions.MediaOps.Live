namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.Element;

	internal abstract class TakeOperationContext
	{
		private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		protected TakeOperationContext(Request request, Endpoint destination)
		{
			Request = request ?? throw new ArgumentNullException(nameof(request));
			Destination = destination ?? throw new ArgumentNullException(nameof(destination));

			if (request.Timeout.HasValue)
			{
				Timeout = request.Timeout.Value;
			}
		}

		public Request Request { get; }

		public Endpoint Destination { get; }

		public IDmsElement DestinationElement { get; set; }

		public MediationElement MediationElement { get; set; }

		public string ConnectionHandlerScript { get; set; }

		public TimeSpan? Timeout { get; set; }

		public bool IsSuccessful { get; set; }

		public Task CompletionTask => _taskCompletionSource.Task;

		public void SetCompleted()
		{
			_taskCompletionSource.TrySetResult(true);
		}
	}
}
