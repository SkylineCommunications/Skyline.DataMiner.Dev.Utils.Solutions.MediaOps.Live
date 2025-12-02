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

		public void LockVirtualSignalGroup(VirtualSignalGroup virtualSignalGroup, string user, string reason, string jobReference, DateTimeOffset? time = null)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			LockVirtualSignalGroups([virtualSignalGroup], user, reason, jobReference, time);
		}

		public void LockVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups, string user, string reason, string jobReference, DateTimeOffset? time = null)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			if (String.IsNullOrWhiteSpace(user))
			{
				throw new ArgumentException($"'{nameof(user)}' cannot be null or whitespace.", nameof(user));
			}

			var requests = virtualSignalGroups
				.Select(vsg => new VirtualSignalGroupLockRequest(vsg, user, reason, jobReference, time))
				.ToList();

			UpdateVirtualSignalGroupLockStates(LockState.Locked, requests);
		}

		public void LockVirtualSignalGroups(ICollection<VirtualSignalGroupLockRequest> lockRequests)
		{
			if (lockRequests is null)
			{
				throw new ArgumentNullException(nameof(lockRequests));
			}

			UpdateVirtualSignalGroupLockStates(LockState.Locked, lockRequests);
		}

		public void ProtectVirtualSignalGroup(VirtualSignalGroup virtualSignalGroup, string user, string reason, string jobReference, DateTimeOffset? time = null)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			if (String.IsNullOrWhiteSpace(user))
			{
				throw new ArgumentException($"'{nameof(user)}' cannot be null or whitespace.", nameof(user));
			}

			ProtectVirtualSignalGroups([virtualSignalGroup], user, reason, jobReference, time);
		}

		public void ProtectVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups, string user, string reason, string jobReference, DateTimeOffset? time = null)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			var requests = virtualSignalGroups
				.Select(vsg => new VirtualSignalGroupLockRequest(vsg, user, reason, jobReference, time))
				.ToList();

			UpdateVirtualSignalGroupLockStates(LockState.Protected, requests);
		}

		public void ProtectVirtualSignalGroups(ICollection<VirtualSignalGroupLockRequest> protectRequests)
		{
			if (protectRequests is null)
			{
				throw new ArgumentNullException(nameof(protectRequests));
			}

			UpdateVirtualSignalGroupLockStates(LockState.Protected, protectRequests);
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

			var requests = virtualSignalGroups.Select(vsg => new VirtualSignalGroupLockRequest(vsg)).ToList();

			UpdateVirtualSignalGroupLockStates(LockState.Unlocked, requests);
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

		private void UpdateVirtualSignalGroupLockStates(LockState lockState, ICollection<VirtualSignalGroupLockRequest> requests)
		{
			if (requests.Count == 0)
			{
				return;
			}

			// Get all virtual signal groups
			var virtualSignalGroups = requests.Select(x => x.VirtualSignalGroup).ToList();

			// Get existing states
			var virtualSignalGroupStates = GetByVirtualSignalGroups(virtualSignalGroups)
				.SafeToDictionary(x => x.VirtualSignalGroupReference);

			// Create a mapping of VSG to request
			var requestsByVsg = requests.SafeToDictionary(x => x.VirtualSignalGroup.ID, x => x);

			// Update or create states
			var statesToUpdate = new List<VirtualSignalGroupState>();

			foreach (var vsg in virtualSignalGroups)
			{
				// Get the request for this VSG
				if (!requestsByVsg.TryGetValue(vsg.ID, out var request))
				{
					continue; // Skip if no request found
				}

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

				if (lockState != LockState.Unlocked && String.IsNullOrWhiteSpace(request.User))
				{
					throw new InvalidOperationException($"A user must be specified when locking or protecting Virtual Signal Group '{vsg.Name}'.");
				}

				// Determine expected values
				var expectedLockedBy = lockState == LockState.Unlocked ? default : request.User;
				var expectedReason = lockState == LockState.Unlocked ? default : request.Reason;
				var expectedJobRef = lockState == LockState.Unlocked ? default : request.JobReference;
				var expectedTime = lockState == LockState.Unlocked ? default : (request.Time ?? DateTimeOffset.UtcNow);

				// Check if an update is needed
				bool needsUpdate =
					state.LockState != lockState ||
					state.LockedBy != expectedLockedBy ||
					state.LockReason != expectedReason ||
					state.LockJobReference != expectedJobRef;

				if (!needsUpdate)
				{
					continue;
				}

				// Apply update
				state.LockState = lockState;
				state.LockedBy = expectedLockedBy;
				state.LockReason = expectedReason;
				state.LockJobReference = expectedJobRef;
				state.LockTime = expectedTime;

				statesToUpdate.Add(state);
			}

			// Save changes
			CreateOrUpdate(statesToUpdate);
		}
	}
}
