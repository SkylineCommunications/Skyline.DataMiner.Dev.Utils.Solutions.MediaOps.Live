namespace Skyline.DataMiner.MediaOps.Live.DOM.Tools
{
	using System;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.SubscriptionFilters;

	public sealed class DomWatcher : IDisposable
	{
		private readonly GQIDMS _dms;
		private IConnection _connection;

		public DomWatcher(string moduleName, GQIDMS dms)
		{
			if (String.IsNullOrWhiteSpace(moduleName))
			{
				throw new ArgumentException($"'{nameof(moduleName)}' cannot be null or whitespace.", nameof(moduleName));
			}

			ModuleName = moduleName;
			_dms = dms ?? throw new ArgumentNullException(nameof(dms));
		}

		public event EventHandler<DomInstancesChangedEventMessage> Changed;

		public string ModuleName { get; }

		public void Start()
		{
			_connection = _dms.GetConnection();
			_connection.OnNewMessage += Connection_OnNewMessage;

			var subscriptionFilter = new ModuleEventSubscriptionFilter<DomInstancesChangedEventMessage>(ModuleName);
			_connection.Subscribe(subscriptionFilter);
		}

		public void Stop()
		{
			try
			{
				_connection.Unsubscribe();
				_connection.OnNewMessage -= Connection_OnNewMessage;
			}
			finally
			{
				_connection.Dispose();
			}
		}

		public void Dispose()
		{
			try
			{
				_connection.Dispose();
			}
			catch (Exception)
			{
				// ignore
			}
		}

		private void Connection_OnNewMessage(object sender, NewMessageEventArgs e)
		{
			if (e.Message is DomInstancesChangedEventMessage domChange)
			{
				Changed?.Invoke(this, domChange);
			}
		}
	}
}
