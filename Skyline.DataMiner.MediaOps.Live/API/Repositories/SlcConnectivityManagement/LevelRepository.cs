namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class LevelRepository : Repository<Level>
	{
		internal LevelRepository(SlcConnectivityManagementHelper helper) : base(helper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => Level.DomDefinition;

		protected override Level CreateInstance(DomInstance domInstance)
		{
			return new Level(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<Level> instances)
		{
			foreach (var instance in instances)
			{
				instance.Validate().ThrowIfInvalid();
			}

			CheckDuplicatesBeforeSave(instances);
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
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.Number), comparer, Convert.ToInt64(value));
				case nameof(Level.Name):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.Name), comparer, (string)value);
				case nameof(Level.TransportType):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.TransportType), comparer, ApiObjectReference<TransportType>.Convert(value));
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

		private void CheckDuplicatesBeforeSave(ICollection<Level> instances)
		{
			FilterElement<DomInstance> CreateFilter(Level l) =>
				DomInstanceExposers.Id.NotEqual(l.ID)
				.AND(
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.Name).Equal(l.Name)
					.OR(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.LevelInfo.Number).Equal(l.Number)));

			var count = FilterQueryExecutor.CountFilteredItems(
				instances,
				x => CreateFilter(x),
				x => Helper.DomInstances.Count(x));

			if (count > 0)
			{
				throw new InvalidOperationException($"Level with same name or number already exists.");
			}
		}

		private void CheckIfStillInUse(ICollection<Level> instances)
		{
			FilterElement<DomInstance> CreateFilter(Level l) =>
				DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevels.Level).Equal(l.ID);

			var count = FilterQueryExecutor.CountFilteredItems(
				instances,
				x => CreateFilter(x),
				x => Helper.DomInstances.Count(x));

			if (count > 0)
			{
				var message = instances.Count == 1
					? $"Cannot delete level '{instances.First().Name}' because it is still in use."
					: "Cannot delete one or more levels because they are still in use.";

				throw new InvalidOperationException(message);
			}
		}
	}
}
