namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Security.Cryptography;
	using System.Text;

	using Microsoft.Extensions.Logging.Abstractions;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.ConnectorAPI.SkylineLockManager.ConnectorApi;
	using Skyline.DataMiner.ConnectorAPI.SkylineLockManager.ConnectorApi.InterApp.Messages.Locking;
	using Skyline.DataMiner.ConnectorAPI.SkylineLockManager.ConnectorApi.InterApp.Messages.Unlocking;
	using Skyline.DataMiner.Net;

	/// <summary>
	/// Ensures the phases of a node are executed by only one execution at a time per job and event, across the whole DataMiner System.
	/// Backed by a lock on the <see cref="LockManagerElementName"/> element. The lock is a lease: it is released automatically
	/// when the owner does not release it in time, for example because its agent went down.
	/// </summary>
	internal sealed class PhaseExecutionGuard : IDisposable
	{
		/// <summary>
		/// Name of the Skyline Lock Manager element that is used to take cluster-wide locks.
		/// </summary>
		internal const string LockManagerElementName = "MOL Lock Manager";

		// The lease outlives the script timeout, so it never expires while the owning execution can still be running.
		internal static readonly TimeSpan LeaseMargin = TimeSpan.FromMinutes(1);

		private static readonly string[] DataMinerProcessNames = { "DataMiner", "SLAutomation", "SLScripting" };
		private static readonly Lazy<bool> IsRunningOnDataMinerAgent = new Lazy<bool>(() =>
		{
			string currentProcessName = Process.GetCurrentProcess().ProcessName;
			return DataMinerProcessNames.Any(x => currentProcessName.StartsWith(x, StringComparison.OrdinalIgnoreCase));
		});

		private readonly IPhaseLocks _locks;
		private readonly string _lockId;
		private bool _disposed;

		private PhaseExecutionGuard(IPhaseLocks locks, string lockId)
		{
			_locks = locks;
			_lockId = lockId;
		}

		/// <summary>
		/// Locks used outside of a DataMiner Agent (e.g. unit tests), where the Lock Manager element cannot be reached.
		/// </summary>
		internal static InMemoryPhaseLocks InMemoryLocks { get; } = new InMemoryPhaseLocks();

		/// <summary>
		/// Tries to take ownership of the phases of the node, without waiting.
		/// </summary>
		/// <returns><see langword="false"/> when another execution currently owns them.</returns>
		internal static bool TryAcquire(IEngine engine, Guid jobInfoId, Guid eventId, string nodeId, out PhaseExecutionGuard guard)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			if (String.IsNullOrEmpty(nodeId))
			{
				throw new ArgumentException($"'{nameof(nodeId)}' cannot be null or empty.", nameof(nodeId));
			}

			IPhaseLocks locks = IsRunningOnDataMinerAgent.Value
				? new LockManagerPhaseLocks(engine.GetUserConnection())
				: (IPhaseLocks)InMemoryLocks;

			TimeSpan lease = (engine.Timeout > TimeSpan.Zero ? engine.Timeout : TimeSpan.Zero) + LeaseMargin;

			return TryAcquire(locks, GetName(jobInfoId, eventId, nodeId), lease, out guard);
		}

		internal static bool TryAcquire(IPhaseLocks locks, string lockId, TimeSpan lease, out PhaseExecutionGuard guard)
		{
			bool acquired;

			try
			{
				acquired = locks.TryLock(lockId, lease);
			}
			catch
			{
				locks.Dispose();
				throw;
			}

			if (!acquired)
			{
				locks.Dispose();
				guard = null;
				return false;
			}

			guard = new PhaseExecutionGuard(locks, lockId);
			return true;
		}

		/// <summary>
		/// Releases ownership. Can be called from any thread.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			try
			{
				_locks.Unlock(_lockId);
			}
			finally
			{
				_locks.Dispose();
			}
		}

		internal static string GetName(Guid jobInfoId, Guid eventId, string nodeId)
		{
			// Node IDs are free text, so they are hashed to keep the lock ID short.
			using (var sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(nodeId));
				string nodeHash = String.Concat(hash.Take(16).Select(b => b.ToString("x2")));

				return $"MediaOpsLive.OrchestrationPhases.{jobInfoId:N}.{eventId:N}.{nodeHash}";
			}
		}

		internal interface IPhaseLocks : IDisposable
		{
			bool TryLock(string lockId, TimeSpan lease);

			void Unlock(string lockId);
		}

		/// <summary>
		/// Cluster-wide locks, taken on the <see cref="LockManagerElementName"/> element.
		/// </summary>
		private sealed class LockManagerPhaseLocks : IPhaseLocks
		{
			private readonly SkylineLockManagerConnectorApi _api;

			public LockManagerPhaseLocks(IConnection connection)
			{
				if (connection == null)
				{
					throw new ArgumentNullException(nameof(connection));
				}

				try
				{
					_api = new SkylineLockManagerConnectorApi(connection, LockManagerElementName, NullLoggerFactory.Instance);
				}
				catch (Exception e)
				{
					throw new InvalidOperationException($"Unable to connect to the Lock Manager element '{LockManagerElementName}', which is needed to execute orchestration phases.", e);
				}
			}

			public bool TryLock(string lockId, TimeSpan lease)
			{
				// Without a maximum waiting time, the Lock Manager answers immediately whether the lock was granted.
				ILockObjectsResult result = _api.LockObject(new LockObjectRequest
				{
					ObjectId = lockId,
					ObjectDescription = "MediaOps Live orchestration phases",
					AutoUnlockTimeSpan = lease,
				});

				return result?.LockInfosPerObjectId != null
					&& result.LockInfosPerObjectId.TryGetValue(lockId, out ILockInfo lockInfo)
					&& lockInfo.IsGranted;
			}

			public void Unlock(string lockId)
			{
				_api.UnlockObject(new UnlockObjectRequest { ObjectId = lockId });
			}

			public void Dispose()
			{
				_api.Dispose();
			}
		}

		/// <summary>
		/// Process-wide locks with the same lease semantics as the Lock Manager.
		/// </summary>
		internal sealed class InMemoryPhaseLocks : IPhaseLocks
		{
			private readonly Dictionary<string, DateTime> _expiryPerLockId = new Dictionary<string, DateTime>();

			public bool TryLock(string lockId, TimeSpan lease)
			{
				lock (_expiryPerLockId)
				{
					DateTime now = DateTime.UtcNow;
					if (_expiryPerLockId.TryGetValue(lockId, out DateTime expiry) && expiry > now)
					{
						return false;
					}

					_expiryPerLockId[lockId] = now + lease;
					return true;
				}
			}

			public void Unlock(string lockId)
			{
				lock (_expiryPerLockId)
				{
					_expiryPerLockId.Remove(lockId);
				}
			}

			public void Dispose()
			{
				// Shared by every execution in the process, so nothing to dispose.
			}
		}
	}
}
