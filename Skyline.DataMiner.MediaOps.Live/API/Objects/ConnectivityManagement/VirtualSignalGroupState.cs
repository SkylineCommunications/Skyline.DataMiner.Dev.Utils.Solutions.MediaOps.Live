namespace Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
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

		public LockState LockState
		{
			get
			{
				if (_domInstance.VirtualSignalGroupLock.LockState.HasValue)
				{
					return (LockState)(int)_domInstance.VirtualSignalGroupLock.LockState.Value;
				}

				return default;
			}

			set
			{
				if (value == LockState.Unlocked)
				{
					_domInstance.VirtualSignalGroupLock.LockState = null;
					return;
				}

				_domInstance.VirtualSignalGroupLock.LockState = (SlcConnectivityManagementIds.Enums.LockState)(int)value;
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
				if (value == DateTimeOffset.MinValue)
				{
					_domInstance.VirtualSignalGroupLock.LockTime = null;
					return;
				}

				_domInstance.VirtualSignalGroupLock.LockTime = value.UtcDateTime;
			}
		}

		public bool IsLocked => LockState == LockState.Locked;

		public bool IsProtected => LockState == LockState.Protected;

		public bool IsUnlocked => LockState == LockState.Unlocked;
	}

	public static class VirtualSignalGroupStateExposers
	{
		public static readonly Exposer<VirtualSignalGroupState, Guid> ID = new(x => x.ID, nameof(VirtualSignalGroupState.ID));
		public static readonly Exposer<VirtualSignalGroupState, ApiObjectReference<VirtualSignalGroup>> VirtualSignalGroupReference = new(x => x.VirtualSignalGroupReference, nameof(VirtualSignalGroupState.VirtualSignalGroupReference));
		public static readonly Exposer<VirtualSignalGroupState, LockState> LockState = new(x => x.LockState, nameof(VirtualSignalGroupState.LockState));
		public static readonly Exposer<VirtualSignalGroupState, string> LockedBy = new(x => x.LockedBy, nameof(VirtualSignalGroupState.LockedBy));
		public static readonly Exposer<VirtualSignalGroupState, string> LockReason = new(x => x.LockReason, nameof(VirtualSignalGroupState.LockReason));
		public static readonly Exposer<VirtualSignalGroupState, string> LockJobReference = new(x => x.LockJobReference, nameof(VirtualSignalGroupState.LockJobReference));
		public static readonly Exposer<VirtualSignalGroupState, DateTimeOffset> LockTime = new(x => x.LockTime, nameof(VirtualSignalGroupState.LockTime));
	}
}
