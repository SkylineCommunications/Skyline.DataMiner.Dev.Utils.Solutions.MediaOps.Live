namespace Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	public class EndpointTransportMetadata
	{
		public EndpointTransportMetadata()
		{
			DomSection = new EndpointTransportMetadataSection();
		}

		internal EndpointTransportMetadata(EndpointTransportMetadataSection domSection)
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
				result.AddError(error, nameof(FieldName));
			}

			return result;
		}

		public override string ToString()
		{
			return $"{FieldName}: {Value}";
		}
	}
}
