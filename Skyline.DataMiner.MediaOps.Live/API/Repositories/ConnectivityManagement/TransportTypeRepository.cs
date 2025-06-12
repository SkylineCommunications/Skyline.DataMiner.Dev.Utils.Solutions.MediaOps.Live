namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
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

		private void CheckIfStillInUse(ICollection<TransportType> instances)
		{
			FilterElement<DomInstance> CreateFilter(TransportType tt) =>
				new ORFilterElement<DomInstance>(
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.TransportType).Equal(tt.ID),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.TransportType).Equal(tt.ID));

			var count = FilterQueryExecutor.CountFilteredItems(
				instances,
				x => CreateFilter(x),
				x => Helper.DomInstances.Count(x));

			if (count > 0)
			{
				var message = instances.Count == 1
					? $"Cannot delete transport type '{instances.First().Name}' because it is still in use."
					: "Cannot delete one or more transport types because they are still in use.";

				throw new InvalidOperationException(message);
			}
		}
	}
}
