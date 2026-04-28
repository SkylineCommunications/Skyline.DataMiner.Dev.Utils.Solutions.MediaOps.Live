namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Tools;

	using SLDataGateway.API.Types.Querying;

	public class TransportTypeRepository : Repository<TransportType>
	{
		internal TransportTypeRepository(MediaOpsLiveApi api) : base(api, api.SlcConnectivityManagementHelper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => TransportType.DomDefinition;

		protected internal override TransportType CreateInstance(DomInstance domInstance)
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

		protected override void ValidateBeforeDelete(ICollection<TransportType> instances)
		{
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
			// Fetch existing DB records that share a name with any instance in the batch.
			static FilterElement<DomInstance> CreateFilter(TransportType tt) =>
				DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeInfo.Name).Equal(tt.Name);

			var existingWithSameName = FilterQueryExecutor.RetrieveFilteredItems(instances, CreateFilter, ReadDom);

			// Build a projected view keyed by ID: DB records as base, overridden by batch entries.
			var transportTypesAfterSave = existingWithSameName.ToDictionary(x => x.ID);

			foreach (var instance in instances)
			{
				transportTypesAfterSave[instance.ID] = instance;
			}

			var duplicateGroups = transportTypesAfterSave.Values
				.GroupBy(x => x.Name)
				.Where(g => g.Count() > 1)
				.ToList();

			if (duplicateGroups.Count > 0)
			{
				var duplicateNames = duplicateGroups.Select(g => g.Key).ToList();
				var names = String.Join(", ", duplicateNames.OrderBy(x => x, new NaturalSortComparer()));
				throw new DuplicateNamesException($"Cannot save transport types. The following names are already in use: {names}", duplicateNames);
			}
		}

		private void CheckIfStillInUse(ICollection<TransportType> transportTypes)
		{
			var transportTypesInUse = new List<TransportType>();
			var referencingLevels = new Dictionary<Guid, Level>();
			var referencingEndpoints = new Dictionary<Guid, Endpoint>();

			foreach (var tt in transportTypes)
			{
				var levels = Api.Levels.Read(LevelExposers.TransportType.UncheckedEqual(tt)).ToList();
				var endpoints = Api.Endpoints.Read(EndpointExposers.TransportType.UncheckedEqual(tt)).ToList();

				if (levels.Count > 0 || endpoints.Count > 0)
				{
					transportTypesInUse.Add(tt);

					foreach (var level in levels)
					{
						referencingLevels[level.ID] = level;
					}

					foreach (var endpoint in endpoints)
					{
						referencingEndpoints[endpoint.ID] = endpoint;
					}
				}
			}

			if (transportTypesInUse.Count > 0)
			{
				throw new TransportTypeInUseException("One or more transport types are still in use", transportTypesInUse, referencingLevels.Values.ToList(), referencingEndpoints.Values.ToList());
			}
		}
	}
}
