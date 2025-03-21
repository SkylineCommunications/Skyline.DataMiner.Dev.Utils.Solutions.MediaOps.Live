namespace Skyline.DataMiner.MediaOps.Live.Mediation
{
	using System;
	using System.Collections.Concurrent;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.SubscriptionFilters;

	using Connection = Skyline.DataMiner.MediaOps.Live.API.Objects.Connection;

	public class ConnectionWatcher : IDisposable
	{
		private readonly string _subscriptionSetName;

		private readonly ConcurrentDictionary<Guid, Connection> _cache = new ConcurrentDictionary<Guid, Connection>();

		public ConnectionWatcher()
		{
			_subscriptionSetName = $"{nameof(ConnectionWatcher)}_{Guid.NewGuid()}";

			Engine.SLNetRaw.OnNewMessage += Connection_OnNewMessage;

			var subscriptionFilter = new ModuleEventSubscriptionFilter<DomInstancesChangedEventMessage>(SlcConnectivityManagementIds.ModuleId);
			Engine.SLNetRaw.AddSubscription(_subscriptionSetName, subscriptionFilter);
		}

		public event EventHandler<Connection> Changed;

		public event EventHandler<Connection> Removed;

		public bool TryGetConnection(Guid destinationId, out Connection connection)
		{
			if (destinationId == Guid.Empty)
			{
				connection = null;
				return false;
			}

			return _cache.TryGetValue(destinationId, out connection);
		}

		public bool TryGetConnection(Endpoint destination, out Connection connection)
		{
			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var destinationId = destination.ID;

			return TryGetConnection(destinationId, out connection);
		}

		public bool IsConnected(Endpoint source, Endpoint destination)
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
				return connection != null && connection.ConnectedSource == source.ID;
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

				var connection = new Connection(instance);

				if (connection.Destination != null)
				{
					_cache.TryRemove((Guid)connection.Destination, out _);
				}

				Removed?.Invoke(this, connection);
			}

			foreach (var instance in domChange.Created.Union(domChange.Updated))
			{
				if (instance.DomDefinitionId.Id != SlcConnectivityManagementIds.Definitions.Connection.Id)
				{
					continue;
				}

				var connection = new Connection(instance);

				if (connection.Destination != null)
				{
					_cache[(Guid)connection.Destination] = connection;
				}

				Changed?.Invoke(this, connection);
			}
		}
	}
}
