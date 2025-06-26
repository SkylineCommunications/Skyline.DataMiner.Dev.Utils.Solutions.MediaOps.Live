namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections.Concurrent;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Async;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.SubscriptionFilters;

	/// <summary>
	/// A mock implementation of <see cref="IConnection"/> used for testing purposes.
	/// </summary>
	public class SLNetConnectionMock : IConnection
	{
		private readonly ConcurrentDictionary<string, SubscriptionSet> _subscriptions = new ConcurrentDictionary<string, SubscriptionSet>();

		/// <summary>
		/// Initializes a new instance of the <see cref="SLNetConnectionMock"/> class.
		/// </summary>
		/// <param name="dms">Mocked DMS.</param>
		internal SLNetConnectionMock(SimulatedDms dms)
		{
			Dms = dms ?? throw new ArgumentNullException(nameof(dms));
		}

		internal SimulatedDms Dms { get; }

		internal void NotifyDomInstancesChanged(DomInstancesChangedEventMessage e)
		{
			foreach (var subscription in _subscriptions.Values)
			{
				var moduleMatch = false;
				var created = e.Created.ToList();
				var updated = e.Updated.ToList();
				var deleted = e.Deleted.ToList();

				foreach (var filter in subscription.Filters)
				{
					switch (filter)
					{
						case ModuleEventSubscriptionFilter<DomInstancesChangedEventMessage> moduleFilter:
							moduleMatch |= moduleFilter.IsMatch(e);
							break;
						case SubscriptionFilter<DomInstancesChangedEventMessage, DomInstance> instanceFilter:
							var lambda = instanceFilter.Filter.getLambda();
							created.RemoveAll(x => !lambda(x));
							updated.RemoveAll(x => !lambda(x));
							deleted.RemoveAll(x => !lambda(x));
							break;
					}
				}

				if (moduleMatch &&
					(created.Count > 0 || updated.Count > 0 || deleted.Count > 0))
				{
					var eventWithSetIds = EventWithSetIDs.Wrap([subscription.SetId], e);

					InvokeOnNewMessageEvent(eventWithSetIds);
				}
			}
		}

		internal void NotifyTableUpdate(ParameterTableUpdateEventMessage e)
		{
			foreach (var subscription in _subscriptions.Values)
			{
				var isFilterMatch = false;

				foreach (var filter in subscription.Filters)
				{
					switch (filter)
					{
						case SubscriptionFilterParameter filterParameter:
							isFilterMatch = filterParameter.ToTypeObject() == typeof(ParameterTableUpdateEventMessage)
								&& filterParameter.DmaID == e.DataMinerID
								&& filterParameter.ElementID == e.ElementID
								&& filterParameter.ParameterID == e.ParameterID;
							break;
					}
				}

				if (isFilterMatch)
				{
					var eventWithSetIds = EventWithSetIDs.Wrap([subscription.SetId], e);

					InvokeOnNewMessageEvent(eventWithSetIds);
				}
			}
		}

		private void InvokeOnNewMessageEvent(DMSMessage message)
		{
			OnNewMessage?.Invoke(
				this,
				new NewMessageEventArgs(message));
		}

		#region IConnection implementation

		/// <inheritdoc/>
		public string UserDomainName => throw new NotImplementedException();

		/// <inheritdoc/>
		public Guid ConnectionID => throw new NotImplementedException();

		/// <inheritdoc/>
		public bool IsShuttingDown => throw new NotImplementedException();

		/// <inheritdoc/>
		public IAsyncMessageHandler Async => throw new NotImplementedException();

		/// <inheritdoc/>
		public bool IsReceiving => throw new NotImplementedException();

		/// <inheritdoc/>
		public ServerDetails ServerDetails => throw new NotImplementedException();

#pragma warning disable CS0067 // The event is never used
		/// <inheritdoc/>
		public event ConnectionClosedHandler OnClose;

		/// <inheritdoc/>
		public event NewMessageEventHandler OnNewMessage;

		/// <inheritdoc/>
		public event AbnormalCloseEventHandler OnAbnormalClose;

		/// <inheritdoc/>
		public event EventsDroppedEventHandler OnEventsDropped;

		/// <inheritdoc/>
		public event SubscriptionCompleteEventHandler OnSubscriptionComplete;

		/// <inheritdoc/>
		public event AuthenticationChallengeEventHandler OnAuthenticationChallenge;

		/// <inheritdoc/>
		public event EventHandler<SubscriptionStateEventArgs> OnSubscriptionState;
#pragma warning restore CS0067

		/// <summary>
		/// Gets a value indicating whether there are subscribers to the <see cref="OnNewMessage"/> event.
		/// For unit testing purposes.
		/// </summary>
		internal bool HasOnNewMessageSubscribers => OnNewMessage?.GetInvocationList().Any() ?? false;

		/// <inheritdoc/>
		public DMSMessage[] HandleMessage(DMSMessage msg)
		{
			if (msg is null)
			{
				throw new ArgumentNullException(nameof(msg));
			}

			if (Dms.TryHandleMessage(msg, out var responses))
			{
				return responses.ToArray();
			}

			return [];
		}

		/// <inheritdoc/>
		public DMSMessage[] HandleMessages(DMSMessage[] msgs)
		{
			if (msgs is null)
			{
				throw new ArgumentNullException(nameof(msgs));
			}

			return msgs.SelectMany(HandleMessage).ToArray();
		}

		/// <inheritdoc/>
		public DMSMessage HandleSingleResponseMessage(DMSMessage msg)
		{
			if (msg is null)
			{
				throw new ArgumentNullException(nameof(msg));
			}

			return HandleMessage(msg).Single();
		}

		/// <inheritdoc/>
		public CreateSubscriptionResponseMessage Subscribe(params SubscriptionFilter[] filters)
		{
			var subscription = _subscriptions.GetOrAdd(String.Empty, x => new SubscriptionSet(x));

			foreach (var filter in filters)
			{
				subscription.Filters.TryAdd(filter);
			}

			return new CreateSubscriptionResponseMessage()
			{
				Filters = filters,
			};
		}

		/// <inheritdoc/>
		public void Unsubscribe()
		{
			_subscriptions.Clear();
		}

		/// <inheritdoc/>
		public void AddSubscription(string setID, params SubscriptionFilter[] newFilters)
		{
			var subscription = _subscriptions.GetOrAdd(setID, x => new SubscriptionSet(x));

			foreach (var filter in newFilters)
			{
				subscription.Filters.TryAdd(filter);
			}
		}

		/// <inheritdoc/>
		public void RemoveSubscription(string setID, params SubscriptionFilter[] deletedFilters)
		{
			var subscription = _subscriptions.GetOrAdd(setID, x => new SubscriptionSet(x));

			foreach (var filter in deletedFilters)
			{
				subscription.Filters.TryRemove(filter);
			}
		}

		/// <inheritdoc/>
		public void ReplaceSubscription(string setID, params SubscriptionFilter[] newFilters)
		{
			ClearSubscriptions(setID);
			AddSubscription(setID);
		}

		/// <inheritdoc/>
		public void ClearSubscriptions(string setID)
		{
			_subscriptions.TryRemove(setID, out _);
		}

		/// <inheritdoc/>
		public ITrackedSubscriptionUpdate TrackSubscribe(params SubscriptionFilter[] filters)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public ITrackedSubscriptionUpdate TrackAddSubscription(string setID, params SubscriptionFilter[] newFilters)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public ITrackedSubscriptionUpdate TrackRemoveSubscription(string setID, params SubscriptionFilter[] deletedFilters)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public ITrackedSubscriptionUpdate TrackReplaceSubscription(string setID, params SubscriptionFilter[] newFilters)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public ITrackedSubscriptionUpdate TrackClearSubscriptions(string setID)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public ITrackedSubscriptionUpdate TrackUpdateSubscription(UpdateSubscriptionMultiMessage multi)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public bool SupportsFeature(CompatibilityFlags flags)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public bool SupportsFeature(string name)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public GetElementProtocolResponseMessage GetElementProtocol(int dmaid, int eid)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public GetProtocolInfoResponseMessage GetProtocol(string name, string version)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public void FireOnAsyncResponse(AsyncResponseEvent responseEvent, ref bool handled)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public DMSMessage[] UnPack(DMSMessage[] messages)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public void SafeWait(int timeout)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Unsubscribe();
		}

		#endregion
	}
}