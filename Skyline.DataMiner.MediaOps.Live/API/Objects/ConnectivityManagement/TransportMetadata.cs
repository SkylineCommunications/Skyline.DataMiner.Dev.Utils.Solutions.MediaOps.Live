namespace Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	public class TransportMetadata
	{
		public TransportMetadata()
		{
			DomSection = new EndpointTransportMetadataSection();
		}

		public TransportMetadata(string fieldName, string value) : this()
		{
			if (String.IsNullOrWhiteSpace(fieldName))
			{
				throw new ArgumentException($"'{nameof(fieldName)}' cannot be null or whitespace.", nameof(fieldName));
			}

			FieldName = fieldName;
			Value = value;
		}

		internal TransportMetadata(EndpointTransportMetadataSection domSection)
		{
			DomSection = domSection ?? throw new ArgumentNullException(nameof(domSection));
		}

		internal EndpointTransportMetadataSection DomSection { get; }

		public string FieldName
		{
			get
			{
				return DomSection.FieldName;
			}

			set
			{
				DomSection.FieldName = value;
			}
		}

		public string Value
		{
			get
			{
				return DomSection.Value;
			}

			set
			{
				DomSection.Value = value;
			}
		}

		public ValidationResult Validate()
		{
			var result = new ValidationResult();

			if (!NameUtil.Validate(FieldName, out var error))
			{
				result.AddError(error, this, x => x.FieldName);
			}

			return result;
		}

		public override string ToString()
		{
			return $"{FieldName}: {Value}";
		}
	}
}
