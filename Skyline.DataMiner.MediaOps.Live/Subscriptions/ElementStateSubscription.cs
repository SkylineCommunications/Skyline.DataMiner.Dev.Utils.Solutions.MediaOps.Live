namespace Skyline.DataMiner.MediaOps.Live.Subscriptions
{
	using System;
	using System.Collections.Concurrent;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	using ElementState = Skyline.DataMiner.Net.Messages.ElementState;

	internal sealed class ElementStateSubscription : IDisposable
	{
		private readonly object _lock = new object();
		private readonly IConnection _connection;

		private readonly string _subscriptionSetId;
		private readonly SubscriptionFilter[] _subscriptionFilters;

		private readonly ConcurrentDictionary<DmsElementId, IDmsElement> _elements = new();
		private readonly ConcurrentDictionary<DmsElementId, ElementState> _elementStates = new();

		public ElementStateSubscription(IConnection connection, bool skipInitialEvents = true)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));

			var subscriptionFilterOptions = skipInitialEvents
				? SubscriptionFilterOptions.SkipInitialEvents
				: SubscriptionFilterOptions.None;

			_subscriptionSetId = $"{nameof(ElementStateSubscription)}_{Guid.NewGuid()}";

			_subscriptionFilters =
			[
				new SubscriptionFilter(typeof(ElementStateEventMessage))
				{
					Options = subscriptionFilterOptions,
				},
			];
		}

		public event EventHandler<ElementStateChange> OnStateChanged
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
						_connection.TrackAddSubscription(_subscriptionSetId, _subscriptionFilters).ExecuteAndWait();
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
						_connection.TrackClearSubscriptions(_subscriptionSetId).ExecuteAndWait();
						_connection.OnNewMessage -= Connection_OnNewMessage;
					}
				}
			}
		}

		private event EventHandler<ElementStateChange> OnStateChanged_Internal;

		public void Dispose()
		{
			_connection.ClearSubscriptions(_subscriptionSetId);
			_connection.OnNewMessage -= Connection_OnNewMessage;

			OnStateChanged_Internal = null;
		}

		private void Connection_OnNewMessage(object sender, NewMessageEventArgs e)
		{
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

		private void HandleElementStateEventMessage(ElementStateEventMessage elementStateEvent)
		{
			if (elementStateEvent.State == ElementState.Active &&
				!elementStateEvent.IsElementStartupComplete)
			{
				// ignore elements that are not fully started yet
				return;
			}

			var elementId = new DmsElementId(elementStateEvent.DataMinerID, elementStateEvent.ElementID);

			if (_elementStates.TryGetValue(elementId, out var previousState) &&
				previousState == elementStateEvent.State)
			{
				// No change in state, ignore
				return;
			}

			_elementStates[elementId] = elementStateEvent.State;

			if (!_elements.TryGetValue(elementId, out var element))
			{
				var dms = _connection.GetDms();
				element = dms.GetElementReference(elementId);
				_elements[elementId] = element;
			}

			var change = new ElementStateChange(element, elementStateEvent.State);
			OnStateChanged_Internal?.Invoke(this, change);
		}
	}
}
