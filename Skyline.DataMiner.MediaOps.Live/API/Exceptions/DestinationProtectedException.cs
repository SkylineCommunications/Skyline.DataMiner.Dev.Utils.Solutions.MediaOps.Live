namespace Skyline.DataMiner.MediaOps.Live.API.Exceptions
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class DestinationProtectedException : Exception
	{
		public DestinationProtectedException()
		{
		}

		public DestinationProtectedException(string message) : base(message)
		{
		}

		public DestinationProtectedException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public DestinationProtectedException(
			VirtualSignalGroup virtualSignalGroup,
			DateTimeOffset lockTime,
			string lockedBy,
			string lockReason)
			: base(GenerateMessage(virtualSignalGroup, lockTime, lockedBy, lockReason))
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
			DateTimeOffset lockTime,
			string lockedBy,
			string lockReason)
		{
			return $"Virtual Signal Group '{virtualSignalGroup?.Name}' is protected by '{lockedBy}'.\n" +
				$"Reason: '{lockReason}'";
		}

		public override string ToString()
		{
			return $"{base.ToString()}, VSG='{VirtualSignalGroup?.Name}', Time={LockTime}, LockedBy='{LockedBy}', Reason='{LockReason}'";
		}
	}
}
