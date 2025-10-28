namespace Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class Endpoint : ApiObject<Endpoint>
	{
		private readonly EndpointInstance _domInstance;

		private readonly WrappedList<EndpointTransportMetadataSection, TransportMetadata> _wrappedTransportMetadata;

		public Endpoint() : this(new EndpointInstance())
		{
		}

		public Endpoint(Guid id) : this(new EndpointInstance(id))
		{
		}

		internal Endpoint(EndpointInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));

			_wrappedTransportMetadata = new WrappedList<EndpointTransportMetadataSection, TransportMetadata>(
				_domInstance.EndpointTransportMetadata,
				x => new TransportMetadata(x),
				x => x.DomSection);
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

		public EndpointRole Role
		{
			get
			{
				if (_domInstance.EndpointInfo.Role.HasValue)
				{
					return (EndpointRole)(int)_domInstance.EndpointInfo.Role.Value;
				}

				return default;
			}

			set
			{
				_domInstance.EndpointInfo.Role = (SlcConnectivityManagementIds.Enums.Role)(int)value;
			}
		}

		public DmsElementId? Element
		{
			get
			{
				if (!String.IsNullOrWhiteSpace(_domInstance.EndpointInfo.Element))
				{
					return new DmsElementId(_domInstance.EndpointInfo.Element);
				}

				return null;
			}

			set
			{
				_domInstance.EndpointInfo.Element = value?.Value;
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

		public DmsElementId? ControlElement
		{
			get
			{
				if (!String.IsNullOrWhiteSpace(_domInstance.EndpointInfo.ControlElement))
				{
					return new DmsElementId(_domInstance.EndpointInfo.ControlElement);
				}

				return null;
			}

			set
			{
				_domInstance.EndpointInfo.ControlElement = value?.Value;
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

		public ApiObjectReference<TransportType> TransportType
		{
			get
			{
				if (_domInstance.EndpointInfo.TransportType.HasValue)
				{
					return _domInstance.EndpointInfo.TransportType.Value;
				}

				return ApiObjectReference<TransportType>.Empty;
			}

			set
			{
				_domInstance.EndpointInfo.TransportType = value;
			}
		}

		public IList<TransportMetadata> TransportMetadata
		{
			get
			{
				return _wrappedTransportMetadata;
			}

			set
			{
				_wrappedTransportMetadata.Clear();
				_wrappedTransportMetadata.AddRange(value);
			}
		}

		public bool IsSource => Role == EndpointRole.Source;

		public bool IsDestination => Role == EndpointRole.Destination;

		public void SetTransportMetadata(string fieldName, string value)
		{
			if (String.IsNullOrWhiteSpace(fieldName))
			{
				throw new ArgumentException($"'{nameof(fieldName)}' cannot be null or whitespace.", nameof(fieldName));
			}

			var existing = TransportMetadata.FirstOrDefault(x => String.Equals(x.FieldName, fieldName));
			if (existing != null)
			{
				existing.Value = value;
			}
			else
			{
				TransportMetadata.Add(new TransportMetadata(fieldName, value));
			}
		}

		public void RemoveTransportMetadata(string fieldName)
		{
			if (String.IsNullOrWhiteSpace(fieldName))
			{
				throw new ArgumentException($"'{nameof(fieldName)}' cannot be null or whitespace.", nameof(fieldName));
			}

			var existing = TransportMetadata.FirstOrDefault(x => String.Equals(x.FieldName, fieldName));
			if (existing != null)
			{
				TransportMetadata.Remove(existing);
			}
		}

		public bool TryGetTransportMetadata(string fieldName, out string value)
		{
			if (String.IsNullOrWhiteSpace(fieldName))
			{
				throw new ArgumentException($"'{nameof(fieldName)}' cannot be null or whitespace.", nameof(fieldName));
			}

			var existing = TransportMetadata.FirstOrDefault(x => String.Equals(x.FieldName, fieldName));
			if (existing != null)
			{
				value = existing.Value;
				return true;
			}

			value = null;
			return false;
		}

		public string GetTransportMetadata(string fieldName)
		{
			if (String.IsNullOrWhiteSpace(fieldName))
			{
				throw new ArgumentException($"'{nameof(fieldName)}' cannot be null or whitespace.", nameof(fieldName));
			}

			if (!TryGetTransportMetadata(fieldName, out var value))
			{
				throw new InvalidOperationException($"No transport metadata found with field name '{fieldName}'.");
			}

			return value;
		}

		public bool HasTransportMetadata(string fieldName, string value)
		{
			if (String.IsNullOrWhiteSpace(fieldName))
			{
				throw new ArgumentException($"'{nameof(fieldName)}' cannot be null or whitespace.", nameof(fieldName));
			}

			return TryGetTransportMetadata(fieldName, out var foundValue) && String.Equals(foundValue, value);
		}

		public ValidationResult Validate()
		{
			var result = new ValidationResult();

			if (!NameUtil.Validate(Name, out var error))
			{
				result.AddError(error, this, x => x.Name);
			}

			if (TransportType == ApiObjectReference<TransportType>.Empty)
			{
				result.AddError($"Transport type is mandatory.", this, x => x.TransportType);
			}

			result.Merge(ValidateTransportMetadata());

			return result;
		}

		private ValidationResult ValidateTransportMetadata()
		{
			var result = new ValidationResult();

			var fieldNames = new HashSet<string>();

			foreach (var metadata in TransportMetadata)
			{
				var metadataResult = metadata.Validate();
				result.Merge(metadataResult);

				if (!fieldNames.Add(metadata.FieldName))
				{
					result.AddError($"Metadata field name '{metadata.FieldName}' is defined multiple times.", this, x => x.TransportMetadata);
				}
			}

			return result;
		}
	}

	public static class EndpointExposers
	{
		public static readonly Exposer<Endpoint, Guid> ID = new Exposer<Endpoint, Guid>(x => x.ID, nameof(Endpoint.ID));
		public static readonly Exposer<Endpoint, string> Name = new Exposer<Endpoint, string>(x => x.Name, nameof(Endpoint.Name));
		public static readonly Exposer<Endpoint, EndpointRole> Role = new Exposer<Endpoint, EndpointRole>(x => x.Role, nameof(Endpoint.Role));
		public static readonly Exposer<Endpoint, DmsElementId?> Element = new Exposer<Endpoint, DmsElementId?>(x => x.Element, nameof(Endpoint.Element));
		public static readonly Exposer<Endpoint, string> Identifier = new Exposer<Endpoint, string>(x => x.Identifier, nameof(Endpoint.Identifier));
		public static readonly Exposer<Endpoint, DmsElementId?> ControlElement = new Exposer<Endpoint, DmsElementId?>(x => x.ControlElement, nameof(Endpoint.ControlElement));
		public static readonly Exposer<Endpoint, string> ControlIdentifier = new Exposer<Endpoint, string>(x => x.ControlIdentifier, nameof(Endpoint.ControlIdentifier));
		public static readonly Exposer<Endpoint, ApiObjectReference<TransportType>?> TransportType = new Exposer<Endpoint, ApiObjectReference<TransportType>?>(x => x.TransportType, nameof(Endpoint.TransportType));
	}
}
