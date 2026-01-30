namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;

	/// <summary>
	/// Represents lock metadata for a single virtual signal group.
	/// </summary>
	public class VirtualSignalGroupLockRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualSignalGroupLockRequest"/> class.
		/// </summary>
		/// <param name="virtualSignalGroup">The virtual signal group to lock.</param>
		/// <param name="user">The user performing the lock.</param>
		/// <param name="reason">The reason for the lock.</param>
		/// <param name="jobReference">The job reference associated with the lock.</param>
		/// <param name="time">The time of the lock. If null, the current UTC time will be used.</param>
		public VirtualSignalGroupLockRequest(
			VirtualSignalGroup virtualSignalGroup,
			string user,
			string reason,
			string jobReference,
			DateTimeOffset? time = null)
		{
			VirtualSignalGroup = virtualSignalGroup ?? throw new ArgumentNullException(nameof(virtualSignalGroup));

			if (String.IsNullOrWhiteSpace(user))
			{
				throw new ArgumentException($"'{nameof(user)}' cannot be null or whitespace.", nameof(user));
			}

			User = user;
			Reason = reason;
			JobReference = jobReference;
			Time = time;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualSignalGroupLockRequest"/> class for unlocking.
		/// </summary>
		/// <param name="virtualSignalGroup">The virtual signal group to unlock.</param>
		public VirtualSignalGroupLockRequest(VirtualSignalGroup virtualSignalGroup)
		{
			VirtualSignalGroup = virtualSignalGroup ?? throw new ArgumentNullException(nameof(virtualSignalGroup));
			User = null;
			Reason = null;
			JobReference = null;
			Time = null;
		}

		/// <summary>
		/// Gets the virtual signal group to lock.
		/// </summary>
		public VirtualSignalGroup VirtualSignalGroup { get; }

		/// <summary>
		/// Gets the user performing the lock.
		/// </summary>
		public string User { get; }

		/// <summary>
		/// Gets the reason for the lock.
		/// </summary>
		public string Reason { get; }

		/// <summary>
		/// Gets the job reference associated with the lock.
		/// </summary>
		public string JobReference { get; }

		/// <summary>
		/// Gets the time of the lock. If null, the current UTC time will be used.
		/// </summary>
		public DateTimeOffset? Time { get; }
	}
}
