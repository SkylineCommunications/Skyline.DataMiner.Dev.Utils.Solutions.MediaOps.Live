namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
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

		public IEnumerable<VirtualSignalGroupState> GetByVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			static FilterElement<VirtualSignalGroupState> BuildFilter(VirtualSignalGroup vsg)
			{
				return VirtualSignalGroupStateExposers.VirtualSignalGroupReference.Equal(vsg.ID);
			}

			return FilterQueryExecutor.RetrieveFilteredItems(virtualSignalGroups, BuildFilter, Read);
		}

		public VirtualSignalGroupState GetByVirtualSignalGroup(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			return GetByVirtualSignalGroups([virtualSignalGroup]).SingleOrDefault();
		}

		public bool TryGetByVirtualSignalGroup(VirtualSignalGroup virtualSignalGroup, out VirtualSignalGroupState virtualSignalGroupState)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			virtualSignalGroupState = GetByVirtualSignalGroup(virtualSignalGroup);

			return virtualSignalGroupState != null;
		}

		public void LockVirtualSignalGroup(VirtualSignalGroup virtualSignalGroup, string user, string reason, string jobReference)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			LockVirtualSignalGroups([virtualSignalGroup], user, reason, jobReference);
		}

		public void LockVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups, string user, string reason, string jobReference)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			if (String.IsNullOrWhiteSpace(user))
			{
				throw new ArgumentException($"'{nameof(user)}' cannot be null or whitespace.", nameof(user));
			}

			UpdateVirtualSignalGroupLockStates(virtualSignalGroups, LockState.Locked, user, reason, jobReference);
		}

		public void ProtectVirtualSignalGroup(VirtualSignalGroup virtualSignalGroup, string user, string reason, string jobReference)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			if (String.IsNullOrWhiteSpace(user))
			{
				throw new ArgumentException($"'{nameof(user)}' cannot be null or whitespace.", nameof(user));
			}

			ProtectVirtualSignalGroups([virtualSignalGroup], user, reason, jobReference);
		}

		public void ProtectVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups, string user, string reason, string jobReference)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			UpdateVirtualSignalGroupLockStates(virtualSignalGroups, LockState.Protected, user, reason, jobReference);
		}

		public void UnlockVirtualSignalGroup(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			UnlockVirtualSignalGroups([virtualSignalGroup]);
		}

		public void UnlockVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			UpdateVirtualSignalGroupLockStates(virtualSignalGroups, LockState.Unlocked, null, null, null);
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
				case nameof(VirtualSignalGroupState.LockState):
					return FilterElementFactory.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockState), comparer, value);
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
				case nameof(VirtualSignalGroupState.LockState):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockState), sortOrder, naturalSort);
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

		private void UpdateVirtualSignalGroupLockStates(ICollection<VirtualSignalGroup> virtualSignalGroups, LockState lockState, string user, string reason, string jobReference)
		{
			if (virtualSignalGroups.Count == 0)
			{
				return;
			}

			// Get existing states
			var virtualSignalGroupStates = GetByVirtualSignalGroups(virtualSignalGroups)
				.SafeToDictionary(x => x.VirtualSignalGroupReference);

			// Update or create states
			var statesToUpdate = new List<VirtualSignalGroupState>();

			foreach (var vsg in virtualSignalGroups)
			{
				// Retrieve or create the state for the current virtual signal group
				if (!virtualSignalGroupStates.TryGetValue(vsg.ID, out var state))
				{
					state = new VirtualSignalGroupState { VirtualSignalGroupReference = vsg.ID };
				}

				// Prevent locking if the virtual signal group is protected
				if (state.IsProtected && lockState == LockState.Locked)
				{
					throw new InvalidOperationException($"Virtual Signal Group '{vsg.Name}' is protected and cannot be locked.");
				}

				// Check if we need to update
				bool needsUpdate = state.LockState != lockState ||
					state.LockedBy != user ||
					state.LockReason != reason ||
					state.LockJobReference != jobReference;

				if (!needsUpdate)
				{
					// No change needed
					continue;
				}

				state.LockState = lockState;
				state.LockedBy = user;
				state.LockReason = reason;
				state.LockJobReference = jobReference;
				state.LockTime = lockState != LockState.Unlocked ? DateTimeOffset.UtcNow : DateTimeOffset.MinValue;

				statesToUpdate.Add(state);
			}

			// Save changes
			CreateOrUpdate(statesToUpdate);
		}
	}
}
