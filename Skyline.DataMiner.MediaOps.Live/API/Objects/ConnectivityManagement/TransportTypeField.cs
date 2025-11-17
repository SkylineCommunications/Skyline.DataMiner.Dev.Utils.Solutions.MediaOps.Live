namespace Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	public sealed class TransportTypeField : IEquatable<TransportTypeField>
	{
		public TransportTypeField()
		{
			DomSection = new TransportTypeFieldSection();
		}

		public TransportTypeField(string name)
		{
			DomSection = new TransportTypeFieldSection
			{
				Name = name,
			};
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

		public override bool Equals(object obj)
		{
			return Equals(obj as TransportTypeField);
		}

		public bool Equals(TransportTypeField other)
		{
			return other is not null &&
				   EqualityComparer<TransportTypeFieldSection>.Default.Equals(DomSection, other.DomSection);
		}

		public override int GetHashCode()
		{
			return EqualityComparer<TransportTypeFieldSection>.Default.GetHashCode(DomSection);
		}

		public static bool operator ==(TransportTypeField left, TransportTypeField right)
		{
			return EqualityComparer<TransportTypeField>.Default.Equals(left, right);
		}

		public static bool operator !=(TransportTypeField left, TransportTypeField right)
		{
			return !(left == right);
		}
	}
}
