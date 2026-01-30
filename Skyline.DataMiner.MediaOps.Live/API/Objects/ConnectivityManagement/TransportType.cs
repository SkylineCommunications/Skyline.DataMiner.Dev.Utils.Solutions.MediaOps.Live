namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	public class TransportType : ApiObject<TransportType>
	{
		private readonly TransportTypeInstance _domInstance;

		private readonly WrappedList<TransportTypeFieldSection, TransportTypeField> _wrappedFields;

		public TransportType() : this(new TransportTypeInstance())
		{
		}

		public TransportType(Guid id) : this(new TransportTypeInstance(id))
		{
		}

		internal TransportType(TransportTypeInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));

			_wrappedFields = new WrappedList<TransportTypeFieldSection, TransportTypeField>(
				_domInstance.TransportTypeField,
				x => new TransportTypeField(x),
				x => x.DomSection);
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

		public IList<TransportTypeField> Fields
		{
			get
			{
				return _wrappedFields;
			}

			set
			{
				_wrappedFields.Clear();
				_wrappedFields.AddRange(value);
			}
		}

		public ValidationResult Validate()
		{
			var result = new ValidationResult();

			if (!NameUtil.Validate(Name, out var error))
			{
				result.AddError(error, this, x => x.Name);
			}

			result.Merge(ValidateMetadataFields());

			return result;
		}

		private ValidationResult ValidateMetadataFields()
		{
			var result = new ValidationResult();

			var fieldNames = new HashSet<string>();

			foreach (var field in Fields)
			{
				var fieldResult = field.Validate();
				result.Merge(fieldResult);

				if (!fieldNames.Add(field.Name))
				{
					result.AddError($"Field name '{field.Name}' is duplicated.", field, x => x.Name);
				}
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
