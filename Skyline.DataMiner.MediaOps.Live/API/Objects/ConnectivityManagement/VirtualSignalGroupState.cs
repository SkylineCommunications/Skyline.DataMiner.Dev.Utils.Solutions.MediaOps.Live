namespace Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class VirtualSignalGroupState : ApiObject<VirtualSignalGroupState>
	{
		private readonly VirtualSignalGroupStateInstance _domInstance;

		public VirtualSignalGroupState() : this(new VirtualSignalGroupStateInstance())
		{
		}

		public VirtualSignalGroupState(Guid id) : this(new VirtualSignalGroupStateInstance(id))
		{
		}

		internal VirtualSignalGroupState(VirtualSignalGroupStateInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal VirtualSignalGroupState(DomInstance domInstance) : this(new VirtualSignalGroupStateInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcConnectivityManagementIds.Definitions.VirtualSignalGroupState;

		public ApiObjectReference<VirtualSignalGroup> VirtualSignalGroupReference
		{
			get
			{
				return _domInstance.VirtualSignalGroupStateInfo.VirtualSignalGroupReference
					?? ApiObjectReference<VirtualSignalGroup>.Empty;
			}

			set
			{
				_domInstance.VirtualSignalGroupStateInfo.VirtualSignalGroupReference = value;
			}
		}

		public bool IsLocked
		{
			get
			{
				return _domInstance.VirtualSignalGroupLock.IsLocked ?? false;
			}

			set
			{
				_domInstance.VirtualSignalGroupLock.IsLocked = value;
			}
		}

		public string LockedBy
		{
			get
			{
				return _domInstance.VirtualSignalGroupLock.LockedBy;
			}

			set
			{
				_domInstance.VirtualSignalGroupLock.LockedBy = value;
			}
		}

		public string LockReason
		{
			get
			{
				return _domInstance.VirtualSignalGroupLock.LockReason;
			}

			set
			{
				_domInstance.VirtualSignalGroupLock.LockReason = value;
			}
		}

		public string LockJobReference
		{
			get
			{
				return _domInstance.VirtualSignalGroupLock.LockJobReference;
			}

			set
			{
				_domInstance.VirtualSignalGroupLock.LockJobReference = value;
			}
		}

		public DateTimeOffset LockTime
		{
			get
			{
				if (_domInstance.VirtualSignalGroupLock.LockTime == null)
				{
					return DateTimeOffset.MinValue;
				}

				return DateTime.SpecifyKind(_domInstance.VirtualSignalGroupLock.LockTime.Value, DateTimeKind.Utc);
			}

			set
			{
				_domInstance.VirtualSignalGroupLock.LockTime = value.UtcDateTime;
			}
		}
	}

	public static class VirtualSignalGroupStateExposers
	{
		public static readonly Exposer<VirtualSignalGroupState, Guid> ID = new Exposer<VirtualSignalGroupState, Guid>(x => x.ID, nameof(VirtualSignalGroupState.ID));
		public static readonly Exposer<VirtualSignalGroupState, ApiObjectReference<VirtualSignalGroup>> VirtualSignalGroupReference = new Exposer<VirtualSignalGroupState, ApiObjectReference<VirtualSignalGroup>>(x => x.VirtualSignalGroupReference, nameof(VirtualSignalGroupState.VirtualSignalGroupReference));
		public static readonly Exposer<VirtualSignalGroupState, bool> IsLocked = new Exposer<VirtualSignalGroupState, bool>(x => x.IsLocked, nameof(VirtualSignalGroupState.IsLocked));
		public static readonly Exposer<VirtualSignalGroupState, string> LockedBy = new Exposer<VirtualSignalGroupState, string>(x => x.LockedBy, nameof(VirtualSignalGroupState.LockedBy));
		public static readonly Exposer<VirtualSignalGroupState, string> LockReason = new Exposer<VirtualSignalGroupState, string>(x => x.LockReason, nameof(VirtualSignalGroupState.LockReason));
		public static readonly Exposer<VirtualSignalGroupState, string> LockJobReference = new Exposer<VirtualSignalGroupState, string>(x => x.LockJobReference, nameof(VirtualSignalGroupState.LockJobReference));
		public static readonly Exposer<VirtualSignalGroupState, DateTimeOffset> LockTime = new Exposer<VirtualSignalGroupState, DateTimeOffset>(x => x.LockTime, nameof(VirtualSignalGroupState.LockTime));
	}
}
