namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class VirtualSignalGroupStateRepository : Repository<VirtualSignalGroupState>
	{
		internal VirtualSignalGroupStateRepository(MediaOpsLiveApi api) : base(api, api.SlcConnectivityManagementHelper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => VirtualSignalGroupState.DomDefinition;

		public ICollection<VirtualSignalGroupState> GetByVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			static FilterElement<VirtualSignalGroupState> BuildFilter(VirtualSignalGroup vsg)
			{
				return VirtualSignalGroupStateExposers.VirtualSignalGroupReference.Equal(vsg.ID);
			}

			var virtualSignalGroupStates = FilterQueryExecutor.RetrieveFilteredItems(virtualSignalGroups, BuildFilter, Read)
				.ToList();

			return virtualSignalGroupStates;
		}

		public bool TryGetByVirtualSignalGroup(VirtualSignalGroup virtualSignalGroup, out VirtualSignalGroupState virtualSignalGroupState)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			virtualSignalGroupState = GetByVirtualSignalGroups([virtualSignalGroup]).SingleOrDefault();

			return virtualSignalGroupState != null;
		}

		public void LockVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups, string user, string reason, string jobReference)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			var virtualSignalGroupStates = GetByVirtualSignalGroups(virtualSignalGroups)
				.SafeToDictionary(x => x.VirtualSignalGroupReference);

			var statesToUpdate = new List<VirtualSignalGroupState>();

			foreach (var vsg in virtualSignalGroups)
			{
				if (!virtualSignalGroupStates.TryGetValue(vsg.ID, out var state))
				{
					state = new VirtualSignalGroupState
					{
						VirtualSignalGroupReference = vsg.ID,
					};
				}

				state.IsLocked = true;
				state.LockedBy = user;
				state.LockReason = reason;
				state.LockJobReference = jobReference;
				state.LockTime = DateTimeOffset.UtcNow;

				statesToUpdate.Add(state);
			}

			CreateOrUpdate(statesToUpdate);
		}

		public void UnlockVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			var virtualSignalGroupStates = GetByVirtualSignalGroups(virtualSignalGroups)
				.SafeToDictionary(x => x.VirtualSignalGroupReference);

			var statesToUpdate = new List<VirtualSignalGroupState>();

			foreach (var vsg in virtualSignalGroups)
			{
				if (!virtualSignalGroupStates.TryGetValue(vsg.ID, out var state))
				{
					state = new VirtualSignalGroupState
					{
						VirtualSignalGroupReference = vsg.ID,
					};
				}

				state.IsLocked = false;
				state.LockedBy = null;
				state.LockReason = null;
				state.LockJobReference = null;
				state.LockTime = DateTimeOffset.MinValue;

				statesToUpdate.Add(state);
			}

			CreateOrUpdate(statesToUpdate);
		}

		public void DeleteByVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			var virtualSignalGroupStates = GetByVirtualSignalGroups(virtualSignalGroups);

			Delete(virtualSignalGroupStates);
		}

		protected internal override VirtualSignalGroupState CreateInstance(DomInstance domInstance)
		{
			return new VirtualSignalGroupState(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<VirtualSignalGroupState> instances)
		{
			// no checks needed
		}

		protected override void ValidateBeforeDelete(ICollection<VirtualSignalGroupState> instances)
		{
			// no checks needed
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(VirtualSignalGroupState.VirtualSignalGroupReference):
					return FilterElementFactory.Create<Guid>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupStateInfo.VirtualSignalGroupReference), comparer, value);
				case nameof(VirtualSignalGroupState.IsLocked):
					return FilterElementFactory.Create<bool>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.IsLocked), comparer, value);
				case nameof(VirtualSignalGroupState.LockedBy):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockedBy), comparer, value);
				case nameof(VirtualSignalGroupState.LockReason):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockReason), comparer, value);
				case nameof(VirtualSignalGroupState.LockJobReference):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockJobReference), comparer, value);
				case nameof(VirtualSignalGroupState.LockTime):
					return FilterElementFactory.Create<DateTimeOffset>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockTime), comparer, value);
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(VirtualSignalGroupState.VirtualSignalGroupReference):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupStateInfo.VirtualSignalGroupReference), sortOrder, naturalSort);
				case nameof(VirtualSignalGroupState.IsLocked):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.IsLocked), sortOrder, naturalSort);
				case nameof(VirtualSignalGroupState.LockedBy):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockedBy), sortOrder, naturalSort);
				case nameof(VirtualSignalGroupState.LockReason):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockReason), sortOrder, naturalSort);
				case nameof(VirtualSignalGroupState.LockJobReference):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockJobReference), sortOrder, naturalSort);
				case nameof(VirtualSignalGroupState.LockTime):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockTime), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}
	}
}
