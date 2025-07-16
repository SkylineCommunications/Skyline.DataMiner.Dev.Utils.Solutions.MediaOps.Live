namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	internal class TrackedSubscriptionUpdate : ITrackedSubscriptionUpdate
	{
		private readonly Action _executeAction;
		private Action _onFinishedAction;

		public TrackedSubscriptionUpdate(Action executeAction)
		{
			_executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
		}

		public int MarkerID => throw new NotImplementedException();

		public DMSMessage[] Execute()
		{
			_executeAction.Invoke();
			_onFinishedAction?.Invoke();

			return [];
		}

		public DMSMessage[] ExecuteAndWait(TimeSpan? timeout = null)
		{
			throw new NotImplementedException();
		}

		public ITrackedSubscriptionUpdate OnAfterInitialEvents(Action action)
		{
			throw new NotImplementedException();
		}

		public ITrackedSubscriptionUpdate OnEndUpdating(Action action)
		{
			throw new NotImplementedException();
		}

		public ITrackedSubscriptionUpdate OnFinished(Action action)
		{
			_onFinishedAction = action ?? throw new ArgumentNullException(nameof(action));

			return this;
		}

		public ITrackedSubscriptionUpdate OnStage(SubscriptionStage stage, Action action)
		{
			throw new NotImplementedException();
		}

		public ITrackedSubscriptionUpdate OnStartUpdating(Action action)
		{
			throw new NotImplementedException();
		}
	}
}
