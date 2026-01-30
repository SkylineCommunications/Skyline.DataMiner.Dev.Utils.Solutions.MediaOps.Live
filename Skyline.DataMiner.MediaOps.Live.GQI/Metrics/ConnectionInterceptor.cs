namespace Skyline.DataMiner.Solutions.MediaOps.Live.GQI.Metrics
{
	using System;
	using System.Diagnostics;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Async;
	using Skyline.DataMiner.Net.Messages;

	public class ConnectionInterceptor : IConnection
	{
		private readonly IConnection _wrappedConnection;

		public ConnectionInterceptor(IConnection connection)
		{
			_wrappedConnection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public event EventHandler<ProcessedMessages> MessagesProcessed;

		#region IConnection Interface

		public string UserDomainName => _wrappedConnection.UserDomainName;

		public Guid ConnectionID => _wrappedConnection.ConnectionID;

		public bool IsShuttingDown => _wrappedConnection.IsShuttingDown;

		public IAsyncMessageHandler Async => _wrappedConnection.Async;

		public bool IsReceiving => _wrappedConnection.IsReceiving;

		public ServerDetails ServerDetails => _wrappedConnection.ServerDetails;

		public event ConnectionClosedHandler OnClose
		{
			add
			{
				_wrappedConnection.OnClose += value;
			}

			remove
			{
				_wrappedConnection.OnClose -= value;
			}
		}

		public event NewMessageEventHandler OnNewMessage
		{
			add
			{
				_wrappedConnection.OnNewMessage += value;
			}

			remove
			{
				_wrappedConnection.OnNewMessage -= value;
			}
		}

		public event AbnormalCloseEventHandler OnAbnormalClose
		{
			add
			{
				_wrappedConnection.OnAbnormalClose += value;
			}

			remove
			{
				_wrappedConnection.OnAbnormalClose -= value;
			}
		}

		public event EventsDroppedEventHandler OnEventsDropped
		{
			add
			{
				_wrappedConnection.OnEventsDropped += value;
			}

			remove
			{
				_wrappedConnection.OnEventsDropped -= value;
			}
		}

		public event SubscriptionCompleteEventHandler OnSubscriptionComplete
		{
			add
			{
				_wrappedConnection.OnSubscriptionComplete += value;
			}

			remove
			{
				_wrappedConnection.OnSubscriptionComplete -= value;
			}
		}

		public event AuthenticationChallengeEventHandler OnAuthenticationChallenge
		{
			add
			{
				_wrappedConnection.OnAuthenticationChallenge += value;
			}

			remove
			{
				_wrappedConnection.OnAuthenticationChallenge -= value;
			}
		}

		public event EventHandler<SubscriptionStateEventArgs> OnSubscriptionState
		{
			add
			{
				_wrappedConnection.OnSubscriptionState += value;
			}

			remove
			{
				_wrappedConnection.OnSubscriptionState -= value;
			}
		}

		public void AddSubscription(string setID, params SubscriptionFilter[] newFilters)
		{
			_wrappedConnection.AddSubscription(setID, newFilters);
		}

		public void ClearSubscriptions(string setID)
		{
			_wrappedConnection.ClearSubscriptions(setID);
		}

		public void Dispose()
		{
			_wrappedConnection.Dispose();
		}

		public void FireOnAsyncResponse(AsyncResponseEvent responseEvent, ref bool handled)
		{
			_wrappedConnection.FireOnAsyncResponse(responseEvent, ref handled);
		}

		public GetElementProtocolResponseMessage GetElementProtocol(int dmaid, int eid)
		{
			return _wrappedConnection.GetElementProtocol(dmaid, eid);
		}

		public GetProtocolInfoResponseMessage GetProtocol(string name, string version)
		{
			return _wrappedConnection.GetProtocol(name, version);
		}

		public DMSMessage[] HandleMessage(DMSMessage msg)
		{
			var stopwatch = Stopwatch.StartNew();

			var responses = _wrappedConnection.HandleMessage(msg);

			MessagesProcessed?.Invoke(this, new ProcessedMessages(new[] { msg }, responses, stopwatch.Elapsed));

			return responses;
		}

		public DMSMessage[] HandleMessages(DMSMessage[] msgs)
		{
			var stopwatch = Stopwatch.StartNew();

			var responses = _wrappedConnection.HandleMessages(msgs);

			MessagesProcessed?.Invoke(this, new ProcessedMessages(msgs, responses, stopwatch.Elapsed));

			return responses;
		}

		public DMSMessage HandleSingleResponseMessage(DMSMessage msg)
		{
			var stopwatch = Stopwatch.StartNew();

			var response = _wrappedConnection.HandleSingleResponseMessage(msg);

			MessagesProcessed?.Invoke(this, new ProcessedMessages(new[] { msg }, new[] { response }, stopwatch.Elapsed));

			return response;
		}

		public void RemoveSubscription(string setID, params SubscriptionFilter[] deletedFilters)
		{
			_wrappedConnection.RemoveSubscription(setID, deletedFilters);
		}

		public void ReplaceSubscription(string setID, params SubscriptionFilter[] newFilters)
		{
			_wrappedConnection.ReplaceSubscription(setID, newFilters);
		}

		public void SafeWait(int timeout)
		{
			_wrappedConnection.SafeWait(timeout);
		}

		public CreateSubscriptionResponseMessage Subscribe(params SubscriptionFilter[] filters)
		{
			return _wrappedConnection.Subscribe(filters);
		}

		public bool SupportsFeature(CompatibilityFlags flags)
		{
			return _wrappedConnection.SupportsFeature(flags);
		}

		public bool SupportsFeature(string name)
		{
			return _wrappedConnection.SupportsFeature(name);
		}

		public ITrackedSubscriptionUpdate TrackAddSubscription(string setID, params SubscriptionFilter[] newFilters)
		{
			return _wrappedConnection.TrackAddSubscription(setID, newFilters);
		}

		public ITrackedSubscriptionUpdate TrackClearSubscriptions(string setID)
		{
			return _wrappedConnection.TrackClearSubscriptions(setID);
		}

		public ITrackedSubscriptionUpdate TrackRemoveSubscription(string setID, params SubscriptionFilter[] deletedFilters)
		{
			return _wrappedConnection.TrackRemoveSubscription(setID, deletedFilters);
		}

		public ITrackedSubscriptionUpdate TrackReplaceSubscription(string setID, params SubscriptionFilter[] newFilters)
		{
			return _wrappedConnection.TrackReplaceSubscription(setID, newFilters);
		}

		public ITrackedSubscriptionUpdate TrackSubscribe(params SubscriptionFilter[] filters)
		{
			return _wrappedConnection.TrackSubscribe(filters);
		}

		public ITrackedSubscriptionUpdate TrackUpdateSubscription(UpdateSubscriptionMultiMessage multi)
		{
			return _wrappedConnection.TrackUpdateSubscription(multi);
		}

		public DMSMessage[] UnPack(DMSMessage[] messages)
		{
			return _wrappedConnection.UnPack(messages);
		}

		public void Unsubscribe()
		{
			_wrappedConnection.Unsubscribe();
		}

		#endregion
	}
}
