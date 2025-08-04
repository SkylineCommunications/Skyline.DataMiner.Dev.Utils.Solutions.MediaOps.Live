namespace Skyline.DataMiner.MediaOps.Live.Subscriptions
{
	using System;
	using System.Collections.Concurrent;
	using System.Diagnostics;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	using ElementState = Skyline.DataMiner.Net.Messages.ElementState;

	internal sealed class ElementStateSubscription : IDisposable
	{
		private readonly object _lock = new object();

		private readonly IConnection _connection;
		private readonly IDms _dms;

		private readonly string _subscriptionSetId;
		private readonly SubscriptionFilter[] _subscriptionFilters;

		private readonly ConcurrentDictionary<DmsElementId, ElementState> _elementStates = new();

		private readonly bool _skipInitialEvents;
		private bool _initialEventsReceived;

		public ElementStateSubscription(IConnection connection, bool skipInitialEvents = true)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_dms = connection.GetDms();

			_skipInitialEvents = skipInitialEvents;
			_subscriptionSetId = $"{nameof(ElementStateSubscription)}_{Guid.NewGuid()}";

			_subscriptionFilters =
			[
				new SubscriptionFilter(typeof(ElementStateEventMessage))
				{
					Options = skipInitialEvents ? SubscriptionFilterOptions.SkipInitialEvents : SubscriptionFilterOptions.None,
				},
			];
		}

		public event EventHandler<ElementStateChangeEvent> OnStateChanged
		{
			add
			{
				lock (_lock)
				{
					var subscribe = OnStateChanged_Internal == null;

					OnStateChanged_Internal += value;

					if (subscribe)
					{
						_connection.OnNewMessage += Connection_OnNewMessage;
						_connection.TrackAddSubscription(_subscriptionSetId, _subscriptionFilters)
							.OnAfterInitialEvents(() => _initialEventsReceived = true)
							.Execute();
					}
				}
			}

			remove
			{
				lock (_lock)
				{
					OnStateChanged_Internal -= value;

					if (OnStateChanged_Internal == null)
					{
						_connection.ClearSubscriptions(_subscriptionSetId);
						_connection.OnNewMessage -= Connection_OnNewMessage;
						_initialEventsReceived = false;
					}
				}
			}
		}

		private event EventHandler<ElementStateChangeEvent> OnStateChanged_Internal;

		public void Dispose()
		{
			_connection.ClearSubscriptions(_subscriptionSetId);
			_connection.OnNewMessage -= Connection_OnNewMessage;

			OnStateChanged_Internal = null;
		}

		private void Connection_OnNewMessage(object sender, NewMessageEventArgs e)
		{
			try
			{
				if (_skipInitialEvents && !_initialEventsReceived)
				{
					return;
				}

				if (!e.FromSet(_subscriptionSetId))
				{
					// Not for our subscription
					return;
				}

				if (e.Message is ElementStateEventMessage elementStateEventMessage)
				{
					HandleElementStateEventMessage(elementStateEventMessage);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Exception in {nameof(ElementStateSubscription)}: {ex}");
			}
		}

		private void HandleElementStateEventMessage(ElementStateEventMessage elementStateEvent)
		{
			var elementId = new DmsElementId(elementStateEvent.DataMinerID, elementStateEvent.ElementID);
			var newState = elementStateEvent.State;

			if (newState == ElementState.Active && !elementStateEvent.IsElementStartupComplete)
			{
				// ignore elements that are not fully started yet
				return;
			}

			if (_elementStates.TryGetValue(elementId, out var previousState) &&
				previousState == newState)
			{
				// No change in state, ignore
				return;
			}

			// Update the state in the dictionary
			_elementStates[elementId] = newState;

			var change = new ElementStateChangeEvent(elementId, newState);
			OnStateChanged_Internal?.Invoke(this, change);
		}
	}
}
