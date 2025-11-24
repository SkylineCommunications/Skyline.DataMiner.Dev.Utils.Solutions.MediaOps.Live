namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class LevelRepository : Repository<Level>
	{
		internal LevelRepository(MediaOpsLiveApi api) : base(api, api.SlcConnectivityManagementHelper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => Level.DomDefinition;

		protected internal override Level CreateInstance(DomInstance domInstance)
		{
			return new Level(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<Level> instances)
		{
			foreach (var instance in instances)
			{
				instance.Validate().ThrowIfInvalid();
			}

			CheckDuplicateNamesBeforeSave(instances);
			CheckDuplicateNumbersBeforeSave(instances);
		}

		protected override void ValidateBeforeDelete(ICollection<Level> instances)
		{
			CheckIfStillInUse(instances);
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(Level.Number):
					return FilterElementFactory.Create<long>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.Number), comparer, value);
				case nameof(Level.Name):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.Name), comparer, value);
				case nameof(Level.TransportType):
					return FilterElementFactory.Create<Guid>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.TransportType), comparer, value);
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(Level.Number):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.Number), sortOrder, naturalSort);
				case nameof(Level.Name):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.Name), sortOrder, naturalSort);
				case nameof(Level.TransportType):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.TransportType), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}

		private void CheckDuplicateNamesBeforeSave(ICollection<Level> instances)
		{
			FilterElement<DomInstance> CreateFilter(Level l) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.Id.NotEqual(l.ID),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.Name).Equal(l.Name));

			var conflicts = FilterQueryExecutor.RetrieveFilteredItems(instances, CreateFilter, Read).ToList();

			if (conflicts.Count > 0)
			{
				var names = String.Join(", ", conflicts
					.Select(x => x.Name)
					.OrderBy(x => x, new NaturalSortComparer()));

				throw new InvalidOperationException($"Cannot save levels. The following names are already in use: {names}");
			}
		}

		private void CheckDuplicateNumbersBeforeSave(ICollection<Level> instances)
		{
			FilterElement<DomInstance> CreateFilter(Level l) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.Id.NotEqual(l.ID),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.Number).Equal(l.Number));

			var conflicts = FilterQueryExecutor.RetrieveFilteredItems(instances, CreateFilter, Read).ToList();

			if (conflicts.Count > 0)
			{
				var numbers = String.Join(", ", conflicts
					.Select(x => x.Number)
					.OrderBy(x => x));

				throw new InvalidOperationException($"Cannot save levels. The following numbers are already in use: {numbers}");
			}
		}

		private void CheckIfStillInUse(ICollection<Level> instances)
		{
			FilterElement<DomInstance> CreateFilter(Level l) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.VirtualSignalGroup.Id),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevel.Level).Equal(l.ID));

			var virtualSignalGroups = FilterQueryExecutor.RetrieveFilteredItems(instances, CreateFilter, Helper.DomInstances.Read);

			if (virtualSignalGroups.Any())
			{
				throw new InvalidOperationException("One or more levels are still in use");
			}
		}
	}
}
