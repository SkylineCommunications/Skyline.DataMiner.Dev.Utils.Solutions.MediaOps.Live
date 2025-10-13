namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.API.TransportTypes;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class TransportTypeRepository : Repository<TransportType>
	{
		internal TransportTypeRepository(SlcConnectivityManagementHelper helper, IConnection connection) : base(helper, connection)
		{
		}

		protected internal override DomDefinitionId DomDefinition => TransportType.DomDefinition;

		public void CreatePredefinedTransportTypes()
		{
			var existing = Read(PredefinedTransportTypes.ById.Keys);
			var missing = PredefinedTransportTypes.All.Except(existing.Values).ToList();

			if (missing.Count > 0)
			{
				CreateOrUpdateWithoutValidation(missing);
			}
		}

		protected internal override TransportType CreateInstance(DomInstance domInstance)
		{
			return new TransportType(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<TransportType> instances)
		{
			if (instances.Any(x => x.IsPredefined))
			{
				throw new InvalidOperationException("Modifying a predefined transport type is not allowed.");
			}

			foreach (var instance in instances)
			{
				instance.Validate().ThrowIfInvalid();
			}

			CheckDuplicatesBeforeSave(instances);
		}

		protected override void ValidateBeforeDelete(ICollection<TransportType> instances)
		{
			if (instances.Any(x => x.IsPredefined))
			{
				throw new InvalidOperationException("Modifying a predefined transport type is not allowed.");
			}

			CheckIfStillInUse(instances);
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(Level.Name):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeInfo.Name), comparer, value);
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
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.Id.NotEqual(tt.ID),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeInfo.Name).Equal(tt.Name));

			var conflicts = FilterQueryExecutor.RetrieveFilteredItems(instances, CreateFilter, Read).ToList();

			if (conflicts.Count > 0)
			{
				var names = String.Join(", ", conflicts
					.Select(x => x.Name)
					.OrderBy(x => x, new NaturalSortComparer()));

				throw new InvalidOperationException($"Cannot save transport types. The following names are already in use: {names}");
			}
		}

		private void CheckIfStillInUse(ICollection<TransportType> instances)
		{
			FilterElement<DomInstance> CreateFilter(TransportType tt) =>
				new ORFilterElement<DomInstance>(
					new ANDFilterElement<DomInstance>(
						DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Level.Id),
						DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.TransportType).Equal(tt.ID)),
					new ANDFilterElement<DomInstance>(
						DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id),
						DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.TransportType).Equal(tt.ID)));

			var count = FilterQueryExecutor.CountFilteredItems(instances, CreateFilter, Helper.DomInstances.Count);

			if (count > 0)
			{
				throw new InvalidOperationException("One or more transport types are still in use");
			}
		}
	}
}
