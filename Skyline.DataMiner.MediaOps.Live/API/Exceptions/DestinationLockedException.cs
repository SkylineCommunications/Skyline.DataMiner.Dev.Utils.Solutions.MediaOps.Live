namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	[Serializable]
	public class DestinationLockedException : Exception
	{
		public DestinationLockedException()
		{
		}

		public DestinationLockedException(string message) : base(message)
		{
		}

		public DestinationLockedException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public DestinationLockedException(
			VirtualSignalGroup virtualSignalGroup,
			DateTimeOffset lockTime,
			string lockedBy,
			string lockReason)
			: base(GenerateMessage(virtualSignalGroup, lockedBy, lockReason))
		{
			VirtualSignalGroup = virtualSignalGroup;
			LockTime = lockTime;
			LockedBy = lockedBy;
			LockReason = lockReason;
		}

		public VirtualSignalGroup VirtualSignalGroup { get; }

		public DateTimeOffset LockTime { get; }

		public string LockedBy { get; }

		public string LockReason { get; }

		private static string GenerateMessage(
			VirtualSignalGroup virtualSignalGroup,
			string lockedBy,
			string lockReason)
		{
			var message = $"Virtual Signal Group '{virtualSignalGroup?.Name}' is locked by '{lockedBy}'.";

			if (!String.IsNullOrEmpty(lockReason))
			{
				message += $"\nReason: '{lockReason}'";
			}

			return message;
		}

		public override string ToString()
		{
			return $"{base.ToString()}, VSG='{VirtualSignalGroup?.Name}', Time={LockTime}, LockedBy='{LockedBy}', Reason='{LockReason}'";
		}
	}
}
