namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class Connection : ApiObject<Connection>
	{
		private readonly ConnectionInstance _domInstance;

		public Connection() : this(new ConnectionInstance())
		{
		}

		internal Connection(ConnectionInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal Connection(DomInstance domInstance) : this(new ConnectionInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcConnectivityManagementIds.Definitions.Connection;

		public ApiObjectReference<Endpoint>? Destination
		{
			get
			{
				return _domInstance.ConnectionInfo.Destination;
			}

			set
			{
				_domInstance.ConnectionInfo.Destination = value;
			}
		}

		public bool IsConnected
		{
			get
			{
				return _domInstance.ConnectionInfo.IsConnected ?? false;
			}

			set
			{
				_domInstance.ConnectionInfo.IsConnected = value;
			}
		}

		public ApiObjectReference<Endpoint>? ConnectedSource
		{
			get
			{
				return _domInstance.ConnectionInfo.ConnectedSource;
			}

			set
			{
				_domInstance.ConnectionInfo.ConnectedSource = value;
			}
		}

		public ApiObjectReference<Endpoint>? PendingConnectedSource
		{
			get
			{
				return _domInstance.ConnectionInfo.PendingConnectedSource;
			}

			set
			{
				_domInstance.ConnectionInfo.PendingConnectedSource = value;
			}
		}

		public ValidationResult Validate()
		{
			var result = new ValidationResult();

			if (Destination == null)
			{
				result.AddError<Connection>($"{nameof(Destination)} cannot be null.", c => c.Destination);
			}

			return result;
		}
	}

	public static class ConnectionExposers
	{
		public static readonly Exposer<Connection, Guid> ID = new Exposer<Connection, Guid>(x => x.ID, nameof(Connection.ID));
		public static readonly Exposer<Connection, ApiObjectReference<Endpoint>?> Destination = new Exposer<Connection, ApiObjectReference<Endpoint>?>(x => x.Destination, nameof(Connection.Destination));
		public static readonly Exposer<Connection, bool> IsConnected = new Exposer<Connection, bool>(x => x.IsConnected, nameof(Connection.IsConnected));
		public static readonly Exposer<Connection, ApiObjectReference<Endpoint>?> ConnectedSource = new Exposer<Connection, ApiObjectReference<Endpoint>?>(x => x.ConnectedSource, nameof(Connection.ConnectedSource));
		public static readonly Exposer<Connection, ApiObjectReference<Endpoint>?> PendingConnectedSource = new Exposer<Connection, ApiObjectReference<Endpoint>?>(x => x.PendingConnectedSource, nameof(Connection.PendingConnectedSource));
	}
}
