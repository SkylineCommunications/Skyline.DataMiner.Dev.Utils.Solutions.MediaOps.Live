namespace Skyline.DataMiner.MediaOps.Live.Mediation
{
	using System;
	using System.Collections.Concurrent;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.SubscriptionFilters;

	public class ConnectionWatcher : IDisposable
	{
		private readonly string _subscriptionSetName;

		private readonly ConcurrentDictionary<Guid, ConnectionInstance> _cache = new ConcurrentDictionary<Guid, ConnectionInstance>();

		public ConnectionWatcher()
		{
			_subscriptionSetName = $"{nameof(ConnectionWatcher)}_{Guid.NewGuid()}";

			Engine.SLNetRaw.OnNewMessage += Connection_OnNewMessage;

			var subscriptionFilter = new ModuleEventSubscriptionFilter<DomInstancesChangedEventMessage>(SlcConnectivityManagementIds.ModuleId);
			Engine.SLNetRaw.AddSubscription(_subscriptionSetName, subscriptionFilter);
		}

		public event EventHandler<ConnectionInstance> Changed;

		public event EventHandler<ConnectionInstance> Removed;

		public bool TryGetConnection(Guid destinationId, out ConnectionInstance connection)
		{
			if (destinationId == Guid.Empty)
			{
				connection = null;
				return false;
			}

			return _cache.TryGetValue(destinationId, out connection);
		}

		public bool TryGetConnection(EndpointInstance destination, out ConnectionInstance connection)
		{
			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var destinationId = destination.ID.Id;

			return TryGetConnection(destinationId, out connection);
		}

		public bool IsConnected(EndpointInstance source, EndpointInstance destination)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (TryGetConnection(destination, out var connection))
			{
				return connection != null && connection.ConnectionInfo.ConnectedSource == source.ID.Id;
			}

			return false;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			try
			{
				Engine.SLNetRaw.ClearSubscriptions(_subscriptionSetName);
				Engine.SLNetRaw.OnNewMessage -= Connection_OnNewMessage;
			}
			catch (Exception)
			{
				// ignore
			}
		}

		private void Connection_OnNewMessage(object sender, NewMessageEventArgs e)
		{
			if (!(e.Message is DomInstancesChangedEventMessage domChange) || domChange.ModuleId != SlcConnectivityManagementIds.ModuleId)
			{
				return;
			}

			foreach (var instance in domChange.Deleted)
			{
				if (instance.DomDefinitionId.Id != SlcConnectivityManagementIds.Definitions.Connection.Id)
				{
					continue;
				}

				var connection = new ConnectionInstance(instance);

				if (connection.ConnectionInfo?.Destination is Guid destinationId)
				{
					_cache.TryRemove(destinationId, out _);
				}

				Removed?.Invoke(this, connection);
			}

			foreach (var instance in domChange.Created.Union(domChange.Updated))
			{
				if (instance.DomDefinitionId.Id != SlcConnectivityManagementIds.Definitions.Connection.Id)
				{
					continue;
				}

				var connection = new ConnectionInstance(instance);

				if (connection.ConnectionInfo?.Destination is Guid destinationId && destinationId != Guid.Empty)
				{
					_cache[destinationId] = connection;
				}

				Changed?.Invoke(this, connection);
			}
		}
	}
}
