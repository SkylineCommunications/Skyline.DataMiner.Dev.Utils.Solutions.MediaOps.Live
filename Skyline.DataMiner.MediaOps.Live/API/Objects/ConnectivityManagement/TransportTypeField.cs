namespace Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	public class TransportTypeField
	{
		public TransportTypeField()
		{
			DomSection = new TransportTypeFieldSection();
		}

		internal TransportTypeField(TransportTypeFieldSection domSection)
		{
			DomSection = domSection ?? throw new ArgumentNullException(nameof(domSection));
		}

		internal TransportTypeFieldSection DomSection { get; }

		public string Name
		{
			get
			{
				return DomSection.Name;
			}

			set
			{
				DomSection.Name = value;
			}
		}

		public ValidationResult Validate()
		{
			var result = new ValidationResult();

			if (!NameUtil.Validate(Name, out var error))
			{
				result.AddError(error, this, x => x.Name);
			}

			return result;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
