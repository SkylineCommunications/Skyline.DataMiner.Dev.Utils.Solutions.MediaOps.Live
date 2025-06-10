namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class Endpoint : ApiObject<Endpoint>
	{
		private readonly EndpointInstance _domInstance;

		public Endpoint() : this(new EndpointInstance())
		{
			TransportType = Guid.NewGuid();
		}

		public Endpoint(Guid id) : this(new EndpointInstance(id))
		{
		}

		internal Endpoint(EndpointInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal Endpoint(DomInstance domInstance) : this(new EndpointInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcConnectivityManagementIds.Definitions.Endpoint;

		public string Name
		{
			get
			{
				return _domInstance.EndpointInfo.Name;
			}

			set
			{
				_domInstance.EndpointInfo.Name = value;
			}
		}

		public Role Role
		{
			get
			{
				return (Role)(int)_domInstance.EndpointInfo.Role;
			}

			set
			{
				_domInstance.EndpointInfo.Role = (SlcConnectivityManagementIds.Enums.Role)(int)value;
			}
		}

		public string Element
		{
			get
			{
				return _domInstance.EndpointInfo.Element;
			}

			set
			{
				_domInstance.EndpointInfo.Element = value;
			}
		}

		public string Identifier
		{
			get
			{
				return _domInstance.EndpointInfo.Identifier;
			}

			set
			{
				_domInstance.EndpointInfo.Identifier = value;
			}
		}

		public string ControlElement
		{
			get
			{
				return _domInstance.EndpointInfo.ControlElement;
			}

			set
			{
				_domInstance.EndpointInfo.ControlElement = value;
			}
		}

		public string ControlIdentifier
		{
			get
			{
				return _domInstance.EndpointInfo.ControlIdentifier;
			}

			set
			{
				_domInstance.EndpointInfo.ControlIdentifier = value;
			}
		}

		public TransportTypeTsoip TransportTypeTSoIP
		{
			get
			{
				return _domInstance.TransportTypeTsoip != null
					? new TransportTypeTsoip(_domInstance.TransportTypeTsoip)
					: null;
			}

			set
			{
				_domInstance.TransportTypeTsoip = value?.DomSection;
			}
		}

		public ApiObjectReference<TransportType>? TransportType
		{
			get
			{
				return _domInstance.EndpointInfo.TransportType;
			}

			set
			{
				_domInstance.EndpointInfo.TransportType = value;
			}
		}

		public bool IsSource => Role == Role.Source;

		public bool IsDestination => Role == Role.Destination;

		public ValidationResult Validate()
		{
			var result = new ValidationResult();

			if (!NameUtil.Validate(Name, out var error))
			{
				result.AddError(error, nameof(Name));
			}

			if (TransportType == null)
			{
				result.AddError($"{nameof(TransportType)} cannot be null.", nameof(TransportType));
			}

			return result;
		}
	}

	public static class EndpointExposers
	{
		public static readonly Exposer<Endpoint, Guid> ID = new Exposer<Endpoint, Guid>(x => x.ID, nameof(Endpoint.ID));
		public static readonly Exposer<Endpoint, string> Name = new Exposer<Endpoint, string>(x => x.Name, nameof(Endpoint.Name));
		public static readonly Exposer<Endpoint, Role> Role = new Exposer<Endpoint, Role>(x => x.Role, nameof(Endpoint.Role));
		public static readonly Exposer<Endpoint, string> Element = new Exposer<Endpoint, string>(x => x.Element, nameof(Endpoint.Element));
		public static readonly Exposer<Endpoint, string> Identifier = new Exposer<Endpoint, string>(x => x.Identifier, nameof(Endpoint.Identifier));
		public static readonly Exposer<Endpoint, string> ControlElement = new Exposer<Endpoint, string>(x => x.ControlElement, nameof(Endpoint.ControlElement));
		public static readonly Exposer<Endpoint, string> ControlIdentifier = new Exposer<Endpoint, string>(x => x.ControlIdentifier, nameof(Endpoint.ControlIdentifier));
		public static readonly Exposer<Endpoint, ApiObjectReference<TransportType>?> TransportType = new Exposer<Endpoint, ApiObjectReference<TransportType>?>(x => x.TransportType, nameof(Endpoint.TransportType));

		public static readonly Exposer<Endpoint, string> Tsoip_MulticastIP = new Exposer<Endpoint, string>(x => x.TransportTypeTSoIP.MulticastIP, nameof(TransportTypeTsoip.MulticastIP));
		public static readonly Exposer<Endpoint, int?> Tsoip_Port = new Exposer<Endpoint, int?>(x => x.TransportTypeTSoIP.Port, nameof(TransportTypeTsoip.Port));
		public static readonly Exposer<Endpoint, string> Tsoip_SourceIP = new Exposer<Endpoint, string>(x => x.TransportTypeTSoIP.SourceIP, nameof(TransportTypeTsoip.SourceIP));
	}
}
