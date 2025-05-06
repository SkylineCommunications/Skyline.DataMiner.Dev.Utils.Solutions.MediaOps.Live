namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class TransportType : ApiObject<TransportType>
	{
		private readonly TransportTypeInstance _domInstance;

		public TransportType() : this(new TransportTypeInstance())
		{
		}

		internal TransportType(TransportTypeInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal TransportType(DomInstance domInstance) : this(new TransportTypeInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcConnectivityManagementIds.Definitions.TransportType;

		public string Name
		{
			get
			{
				return _domInstance.TransportTypeInfo.Name;
			}

			set
			{
				_domInstance.TransportTypeInfo.Name = value;
			}
		}

		public void Validate()
		{
			if (String.IsNullOrWhiteSpace(Name))
			{
				throw new InvalidOperationException($"{nameof(Name)} cannot be null, empty, or whitespace.");
			}
		}
	}

	public static class TransportTypeExposers
	{
		public static readonly Exposer<TransportType, Guid> ID = new Exposer<TransportType, Guid>(x => x.ID, nameof(TransportType.ID));
		public static readonly Exposer<TransportType, string> Name = new Exposer<TransportType, string>(x => x.Name, nameof(TransportType.Name));
	}
}
