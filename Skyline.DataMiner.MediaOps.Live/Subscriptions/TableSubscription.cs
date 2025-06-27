namespace Skyline.DataMiner.MediaOps.Live.Subscriptions
{
	using System;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	internal sealed class TableSubscription : IDisposable
	{
		private readonly object _lock = new object();
		private readonly IConnection _connection;
		private readonly IDmsElement _element;
		private readonly int _tableId;

		private readonly string _subscriptionSetId;
		private readonly SubscriptionFilter[] _subscriptionFilters;

		private readonly TableCache _cache;

		public TableSubscription(IConnection connection, IDmsElement element, int tableId)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_element = element ?? throw new ArgumentNullException(nameof(element));
			_tableId = tableId;

			_cache = new TableCache(element, tableId);

			_subscriptionSetId = $"TableSubscription_{_element.DmsElementId.Value}_{_tableId}";
			_subscriptionFilters =
			[
				new SubscriptionFilterParameter(typeof(ParameterTableUpdateEventMessage), _element.AgentId, _element.Id, _tableId),
				new SubscriptionFilterParameter(typeof(ParameterChangeEventMessage), _element.AgentId, _element.Id, _tableId)
				{
					Filters = ["forceFullTable=true"],
				},
			];
		}

		public event EventHandler<TableValueChange> OnChanged
		{
			add
			{
				lock (_lock)
				{
					var subscribe = Changed == null;

					Changed += value;

					if (subscribe)
					{
						_connection.OnNewMessage += Connection_OnNewMessage;
						_connection.AddSubscription(_subscriptionSetId, _subscriptionFilters);
						_connection.Subscribe();
					}
				}
			}

			remove
			{
				lock (_lock)
				{
					Changed -= value;

					if (Changed == null)
					{
						_connection.ClearSubscriptions(_subscriptionSetId);
						_connection.OnNewMessage -= Connection_OnNewMessage;
					}
				}
			}
		}

		private event EventHandler<TableValueChange> Changed;

		public void Dispose()
		{
			_connection.ClearSubscriptions(_subscriptionSetId);
			_connection.OnNewMessage -= Connection_OnNewMessage;

			Changed = null;
		}

		private void Connection_OnNewMessage(object sender, NewMessageEventArgs e)
		{
			if (!e.FromSet(_subscriptionSetId))
			{
				// Not for our subscription
				return;
			}

			if (e.Message is ParameterChangeEventMessage parameterChangeEventMessage)
			{
				HandleParameterChangeEventMessage(parameterChangeEventMessage);
			}
		}

		private void HandleParameterChangeEventMessage(ParameterChangeEventMessage parameterChangeEventMessage)
		{
			var change = _cache.ApplyUpdate(parameterChangeEventMessage);

			if (change.UpdatedRows.Count > 0 || change.DeletedRows.Count > 0)
			{
				Changed?.Invoke(this, change);
			}
		}
	}
}
