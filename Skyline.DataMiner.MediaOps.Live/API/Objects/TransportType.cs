namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class TransportType : ApiObject<TransportType>
	{
		private static readonly string[] _predefinedTransportTypeNames = new[]
		{
			"IP",
			"SDI",
			"TSoIP",
			"SRT",
		};

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

		public bool IsPredefined => _predefinedTransportTypeNames.Contains(Name);

		public string Name
		{
			get
			{
				return _domInstance.TransportTypeInfo.Name;
			}

			set
			{
				if (IsPredefined)
				{
					throw new InvalidOperationException("Name of predefined transport types cannot be modified.");
				}

				_domInstance.TransportTypeInfo.Name = value;
			}
		}

		public ValidationResult Validate()
		{
			var result = new ValidationResult();

			if (!NameUtil.Validate(Name, out var error))
			{
				result.AddError(error, nameof(Name));
			}

			return result;
		}
	}

	public static class TransportTypeExposers
	{
		public static readonly Exposer<TransportType, Guid> ID = new Exposer<TransportType, Guid>(x => x.ID, nameof(TransportType.ID));
		public static readonly Exposer<TransportType, string> Name = new Exposer<TransportType, string>(x => x.Name, nameof(TransportType.Name));
	}
}
