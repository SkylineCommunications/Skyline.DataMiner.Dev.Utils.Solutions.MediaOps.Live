namespace Skyline.DataMiner.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class TransportTypeRepository : Repository<TransportType>
	{
		public TransportTypeRepository(SlcConnectivityManagementHelper helper) : base(helper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => TransportType.DomDefinition;

		protected override TransportType CreateInstance(DomInstance domInstance)
		{
			return new TransportType(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<TransportType> instances)
		{
			foreach (var instance in instances)
			{
				instance.Validate().ThrowIfInvalid();
			}

			CheckDuplicatesBeforeSave(instances);
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(Level.Name):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeInfo.Name), comparer, (string)value);
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(Level.Name):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeInfo.Name), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}

		private void CheckDuplicatesBeforeSave(ICollection<TransportType> instances)
		{
			FilterElement<DomInstance> CreateFilter(TransportType tt) =>
				DomInstanceExposers.Id.NotEqual(tt.ID)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeInfo.Name).Equal(tt.Name));

			var count = FilterQueryExecutor.CountFilteredItems(
				instances,
				x => CreateFilter(x),
				x => Helper.DomInstances.Count(x));

			if (count > 0)
			{
				throw new InvalidOperationException($"Transport type with same name already exists.");
			}
		}
	}
}
